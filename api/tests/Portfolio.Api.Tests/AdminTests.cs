using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Portfolio.Api.Tests;

[Collection(ApiCollection.Name)]
public class AdminTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AdminTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.Email.Reset();
    }

    private static object Credentials(string password = ApiFactory.AdminPassword) =>
        new { email = ApiFactory.AdminEmail, password };

    private static async Task<string> TokenFrom(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/login", Credentials());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>Um cliente que já manda o token em toda requisição.</summary>
    private async Task<HttpClient> LoggedIn()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenFrom(client));
        return client;
    }

    /// <summary>Envia uma mensagem pelo formulário público e devolve o e-mail usado, para achá-la depois.</summary>
    private async Task<string> SendMessage()
    {
        var email = $"ana-{Guid.NewGuid():N}@empresa.test";
        var response = await _client.PostAsJsonAsync("/api/contact",
            new { name = "Ana Recrutadora", email, message = "Olá, quero conversar sobre uma vaga." });
        response.EnsureSuccessStatusCode();
        return email;
    }

    private static async Task<JsonElement> MessageFrom(HttpClient admin, string email)
    {
        var messages = await admin.GetFromJsonAsync<JsonElement>("/api/admin/messages");
        return messages.EnumerateArray().Single(message => message.GetProperty("email").GetString() == email);
    }

    [Fact]
    public async Task Login_Valid_ReturnsTokenWithExpiry()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/login", Credentials());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Um JWT tem três partes separadas por ponto: cabeçalho, dados e assinatura.
        Assert.Equal(3, body.GetProperty("token").GetString()!.Split('.').Length);
        Assert.True(body.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(ApiFactory.AdminEmail, "senha errada")]
    [InlineData("outra@pessoa.test", ApiFactory.AdminPassword)]
    public async Task Login_WrongCredentials_Returns401WithoutSayingWhich(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // A mesma resposta para e-mail errado e para senha errada: não dá pista a quem tenta adivinhar.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("E-mail ou senha incorretos.", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_AboveLimit_Returns429()
    {
        // Limite de tentativas por IP: é o que segura quem tenta adivinhar a senha.
        using var limited = _factory.WithWebHostBuilder(builder => builder.UseSetting("Admin:MaxLoginsPerWindow", "2"));
        var client = limited.CreateClient();

        var first = await client.PostAsJsonAsync("/api/admin/login", Credentials("errada 1"));
        var second = await client.PostAsJsonAsync("/api/admin/login", Credentials("errada 2"));
        var third = await client.PostAsJsonAsync("/api/admin/login", Credentials());

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Login_NotConfigured_Returns503()
    {
        // Sem senha configurada ninguém entra, seja qual for a senha enviada.
        using var unconfigured = _factory.WithWebHostBuilder(builder => builder.UseSetting("Admin:Password", ""));

        var response = await unconfigured.CreateClient().PostAsJsonAsync("/api/admin/login", Credentials("qualquer"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/messages")]
    [InlineData("/api/admin/overview")]
    public async Task Admin_WithoutToken_Returns401(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_TamperedToken_Returns401()
    {
        // Trocar o fim do token estraga a assinatura: a API recusa, mesmo com os dados intactos.
        var token = await TokenFrom(_client);
        var tampered = token[..^4] + (token.EndsWith("AAAA") ? "BBBB" : "AAAA");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Messages_ListsWhatTheFormStored_NewestFirst()
    {
        var older = await SendMessage();
        var newer = await SendMessage();
        var admin = await LoggedIn();

        var messages = (await admin.GetFromJsonAsync<JsonElement>("/api/admin/messages")).EnumerateArray().ToList();

        var emails = messages.Select(message => message.GetProperty("email").GetString()).ToList();
        Assert.True(emails.IndexOf(newer) < emails.IndexOf(older));

        var message = messages[emails.IndexOf(newer)];
        Assert.Equal("Ana Recrutadora", message.GetProperty("name").GetString());
        Assert.Equal("Olá, quero conversar sobre uma vaga.", message.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("readAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, message.GetProperty("notifiedAt").ValueKind);
    }

    [Fact]
    public async Task Patch_MarksAsReadAndBackToUnread()
    {
        var email = await SendMessage();
        var admin = await LoggedIn();
        var id = (await MessageFrom(admin, email)).GetProperty("id").GetInt32();

        var read = await admin.PatchAsJsonAsync($"/api/admin/messages/{id}", new { read = true });
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.NotEqual(JsonValueKind.Null, (await MessageFrom(admin, email)).GetProperty("readAt").ValueKind);

        var unread = await admin.PatchAsJsonAsync($"/api/admin/messages/{id}", new { read = false });
        Assert.Equal(HttpStatusCode.NoContent, unread.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await MessageFrom(admin, email)).GetProperty("readAt").ValueKind);
    }

    [Fact]
    public async Task Delete_RemovesTheMessage_AndUnknownIdIs404()
    {
        var email = await SendMessage();
        var admin = await LoggedIn();
        var id = (await MessageFrom(admin, email)).GetProperty("id").GetInt32();

        var deleted = await admin.DeleteAsync($"/api/admin/messages/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var messages = await admin.GetFromJsonAsync<JsonElement>("/api/admin/messages");
        Assert.DoesNotContain(messages.EnumerateArray(), message => message.GetProperty("email").GetString() == email);

        var again = await admin.DeleteAsync($"/api/admin/messages/{id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Overview_CountsMessagesAndContent()
    {
        var admin = await LoggedIn();
        var before = await admin.GetFromJsonAsync<JsonElement>("/api/admin/overview");

        await SendMessage();
        var after = await admin.GetFromJsonAsync<JsonElement>("/api/admin/overview");

        Assert.Equal(before.GetProperty("messages").GetInt32() + 1, after.GetProperty("messages").GetInt32());
        Assert.Equal(before.GetProperty("unread").GetInt32() + 1, after.GetProperty("unread").GetInt32());

        // O conteúdo de teste (TestContent) tem pelo menos um projeto e uma tecnologia.
        Assert.True(after.GetProperty("projects").GetInt32() > 0);
        Assert.True(after.GetProperty("technologies").GetInt32() > 0);
    }

    [Fact]
    public async Task Cors_Preflight_AllowsThePanelToSendTheToken()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin/messages/1");
        request.Headers.Add("Origin", ApiFactory.SiteOrigin);
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(ApiFactory.SiteOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("PATCH", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single(),
            StringComparison.OrdinalIgnoreCase);
    }
}
