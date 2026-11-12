using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Data;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Tests;

[Collection(ApiCollection.Name)]
public class ContactTests
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ContactTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.Email.Reset();
    }

    /// <summary>Cada teste usa um e-mail próprio para achar só a mensagem que ele mesmo enviou.</summary>
    private static string NewEmail() => $"ana-{Guid.NewGuid():N}@empresa.test";

    private static object Message(string email, string? website = null) => new
    {
        name = "  Ana Recrutadora ",
        email,
        message = "Olá, vi seu portfólio e quero conversar sobre uma vaga.",
        website,
    };

    private async Task<List<ContactMessage>> StoredFrom(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        return await db.ContactMessages.Where(message => message.Email == email).ToListAsync();
    }

    [Fact]
    public async Task Post_Valid_StoresMessageAndSendsEmail()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync("/api/contact", Message(email));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var stored = Assert.Single(await StoredFrom(email));
        Assert.Equal("Ana Recrutadora", stored.Name);
        Assert.NotNull(stored.NotifiedAt);

        var sent = Assert.Single(_factory.Email.Sent);
        Assert.Equal("https://api.resend.com/emails", sent.Url);
        Assert.Equal($"Bearer {ApiFactory.EmailApiKey}", sent.Authorization);

        var payload = JsonDocument.Parse(sent.Body).RootElement;
        Assert.Equal(ApiFactory.EmailTo, payload.GetProperty("to")[0].GetString());
        // Responder ao aviso responde direto para quem escreveu.
        Assert.Equal(email, payload.GetProperty("reply_to").GetString());
        Assert.Contains("Ana Recrutadora", payload.GetProperty("subject").GetString());
        Assert.Contains("quero conversar sobre uma vaga", payload.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Post_Invalid_Returns400NamingEachField()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/contact", new { name = " ", email = "isto não é e-mail", message = "curta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // As chaves saem com o mesmo nome dos campos do JSON, para o site saber onde mostrar.
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _));
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("message", out _));

        Assert.Empty(await StoredFrom("isto não é e-mail"));
        Assert.Empty(_factory.Email.Sent);
    }

    [Fact]
    public async Task Post_Honeypot_AnswersSuccessButStoresNothing()
    {
        // "website" é o campo-isca: fica escondido no formulário e só robô preenche.
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync("/api/contact", Message(email, website: "https://spam.test"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await StoredFrom(email));
        Assert.Empty(_factory.Email.Sent);
    }

    [Fact]
    public async Task Post_EmailProviderFails_StillStoresMessage()
    {
        var email = NewEmail();
        _factory.Email.Response = HttpStatusCode.InternalServerError;

        var response = await _client.PostAsJsonAsync("/api/contact", Message(email));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // A mensagem está no banco, marcada como ainda não avisada.
        var stored = Assert.Single(await StoredFrom(email));
        Assert.Null(stored.NotifiedAt);
    }

    [Fact]
    public async Task Post_AboveLimit_Returns429()
    {
        // Uma segunda instância da API, no mesmo banco, com limite de 2 mensagens por janela.
        using var limited = _factory.WithWebHostBuilder(builder => builder.UseSetting("Contact:MaxPerWindow", "2"));
        var client = limited.CreateClient();

        var first = await client.PostAsJsonAsync("/api/contact", Message(NewEmail()));
        var second = await client.PostAsJsonAsync("/api/contact", Message(NewEmail()));
        var third = await client.PostAsJsonAsync("/api/contact", Message(NewEmail()));

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Cors_Preflight_AllowsPostFromSite()
    {
        // Antes de um POST com JSON, o navegador pergunta à API se pode (requisição OPTIONS).
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/contact");
        request.Headers.Add("Origin", ApiFactory.SiteOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(ApiFactory.SiteOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
    }
}
