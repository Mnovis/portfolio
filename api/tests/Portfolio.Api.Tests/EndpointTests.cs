using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Portfolio.Api.Tests;

[Collection(ApiCollection.Name)]
public class EndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<JsonElement> GetJson(string url) =>
        await _client.GetFromJsonAsync<JsonElement>(url);

    private static List<string?> Strings(JsonElement array, string property) =>
        array.EnumerateArray().Select(item => item.GetProperty(property).GetString()).ToList();

    private static List<string?> Strings(JsonElement array) =>
        array.EnumerateArray().Select(item => item.GetString()).ToList();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> GetFromOrigin(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Add("Origin", origin);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Cors_SiteOrigin_IsAllowed()
    {
        var response = await GetFromOrigin(ApiFactory.SiteOrigin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApiFactory.SiteOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Cors_OtherOrigin_GetsNoPermission()
    {
        // A resposta sai, mas sem o cabeçalho: o navegador de outro site não entrega o conteúdo.
        var response = await GetFromOrigin("https://outro-site.test");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task OpenApi_DocumentListsTheRoutes()
    {
        var document = await GetJson("/openapi/v1.json");

        var paths = document.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        Assert.Contains("/api/profile", paths);
        Assert.Contains("/api/projects", paths);
        Assert.Contains("/api/projects/{slug}", paths);
        Assert.Contains("/api/education", paths);
        Assert.Contains("/api/technologies", paths);
    }

    [Fact]
    public async Task Docs_ServesTheApiReference()
    {
        var response = await _client.GetAsync("/docs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Profile_ReturnsProfileFromContent()
    {
        var profile = await GetJson("/api/profile");

        Assert.Equal("Pessoa de Teste", profile.GetProperty("shortName").GetString());
        Assert.True(profile.GetProperty("remote").GetBoolean());
        Assert.Equal("https://example.com/github", profile.GetProperty("links").GetProperty("github").GetString());
    }

    [Fact]
    public async Task Projects_ReturnsListInFileOrder()
    {
        var projects = await GetJson("/api/projects");

        Assert.Equal(["alpha", "beta"], Strings(projects, "slug"));

        var alpha = projects[0];
        Assert.Equal("InProgress", alpha.GetProperty("status").GetString());
        Assert.True(alpha.GetProperty("featured").GetBoolean());
        // Tecnologias saem na ordem de technologies.json, não na ordem escrita no projeto.
        Assert.Equal(["C#", "PostgreSQL"], Strings(alpha.GetProperty("technologies")));
    }

    [Fact]
    public async Task Project_BySlug_ReturnsDetail()
    {
        var alpha = await GetJson("/api/projects/alpha");

        Assert.Equal("Projeto Alpha", alpha.GetProperty("name").GetString());
        Assert.Equal("Contexto do alpha.", alpha.GetProperty("context").GetString());
        Assert.Equal(["Primeiro destaque", "Segundo destaque"], Strings(alpha.GetProperty("highlights")));
        Assert.Equal("https://example.com/alpha", alpha.GetProperty("repoUrl").GetString());
        Assert.Equal(JsonValueKind.Null, alpha.GetProperty("liveUrl").ValueKind);
        Assert.Equal(["C#", "PostgreSQL"], Strings(alpha.GetProperty("technologies")));
    }

    [Theory]
    [InlineData("/api/projects/nao-existe")]
    [InlineData("/api/projects/ALPHA")]
    [InlineData("/api/rota-que-nao-existe")]
    public async Task NotFound_ReturnsProblemDetails(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Education_ReturnsListInFileOrder()
    {
        var education = await GetJson("/api/education");

        Assert.Equal(["Curso A", "Curso B"], Strings(education, "title"));
        Assert.Equal(["InProgress", "Done"], Strings(education, "status"));
    }

    [Fact]
    public async Task Technologies_ProvenComesFromProjects()
    {
        var technologies = await GetJson("/api/technologies");

        Assert.Equal(["C#", "PostgreSQL", "AWS"], Strings(technologies, "name"));

        var csharp = technologies[0];
        Assert.True(csharp.GetProperty("proven").GetBoolean());
        Assert.True(csharp.GetProperty("studying").GetBoolean());
        Assert.Equal(["alpha"], Strings(csharp.GetProperty("projects"), "slug"));

        var postgres = technologies[1];
        Assert.Equal("Data", postgres.GetProperty("category").GetString());
        Assert.Equal(["alpha", "beta"], Strings(postgres.GetProperty("projects"), "slug"));

        // Estudando, sem projeto: aparece, mas não como comprovada.
        var aws = technologies[2];
        Assert.False(aws.GetProperty("proven").GetBoolean());
        Assert.True(aws.GetProperty("studying").GetBoolean());
        Assert.Empty(aws.GetProperty("projects").EnumerateArray());
    }
}
