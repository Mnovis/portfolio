using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Content:Path", TestContentPath);
        builder.UseSetting("Cors:Origins:0", SiteOrigin);
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

/// <summary>Uma API e um banco compartilhados por todas as classes de teste.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
