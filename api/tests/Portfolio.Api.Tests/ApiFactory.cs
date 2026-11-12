using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Services;
using Testcontainers.PostgreSql;

namespace Portfolio.Api.Tests;

/// <summary>
/// Sobe a API inteira em memória, apontando para um PostgreSQL real em contêiner
/// e para o conteúdo de teste (pasta TestContent).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public static string TestContentPath => Path.Combine(AppContext.BaseDirectory, "TestContent");

    /// <summary>Origem do site nos testes: a única liberada no CORS.</summary>
    public const string SiteOrigin = "https://site.test";

    public const string EmailApiKey = "chave-de-teste";
    public const string EmailTo = "dono@site.test";

    /// <summary>Fica no lugar do provedor de e-mail. Nenhum teste envia e-mail de verdade.</summary>
    public FakeEmailProvider Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Content:Path", TestContentPath);
        builder.UseSetting("Cors:Origins:0", SiteOrigin);
        builder.UseSetting("Email:ApiKey", EmailApiKey);
        builder.UseSetting("Email:To", EmailTo);

        // Os testes compartilham o mesmo "IP"; o limite real é exercitado em um teste próprio.
        builder.UseSetting("Contact:MaxPerWindow", "1000");

        builder.ConfigureTestServices(services =>
            services.AddHttpClient<ContactNotifier>().ConfigurePrimaryHttpMessageHandler(Email.CreateHandler));
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

/// <summary>Guarda o que a API tentou enviar e responde o que o teste mandar.</summary>
public class FakeEmailProvider
{
    public record SentEmail(string? Url, string? Authorization, string Body);

    public List<SentEmail> Sent { get; } = [];
    public HttpStatusCode Response { get; set; } = HttpStatusCode.OK;

    public void Reset()
    {
        Sent.Clear();
        Response = HttpStatusCode.OK;
    }

    public HttpMessageHandler CreateHandler() => new Handler(this);

    private sealed class Handler(FakeEmailProvider provider) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            provider.Sent.Add(new SentEmail(
                request.RequestUri?.ToString(), request.Headers.Authorization?.ToString(), body));

            return new HttpResponseMessage(provider.Response) { Content = new StringContent("""{"id":"teste"}""") };
        }
    }
}

/// <summary>Uma API e um banco compartilhados por todas as classes de teste.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
