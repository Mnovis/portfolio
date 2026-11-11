using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Portfolio.Api.Data;

namespace Portfolio.Api.Tests;

[Collection(ApiCollection.Name)]
public class ContentTests(ApiFactory factory)
{
    private static ContentReader ReaderFor(string path) =>
        new(Options.Create(new ContentOptions { Path = path }));

    /// <summary>Copia o conteúdo de teste para uma pasta temporária, para o teste estragar à vontade.</summary>
    private static string CopyOfTestContent()
    {
        var directory = Directory.CreateTempSubdirectory("portfolio-content-").FullName;
        foreach (var file in Directory.GetFiles(ApiFactory.TestContentPath))
            File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
        return directory;
    }

    private static void Replace(string directory, string fileName, string from, string to)
    {
        var path = Path.Combine(directory, fileName);
        var text = File.ReadAllText(path);
        Assert.Contains(from, text);
        File.WriteAllText(path, text.Replace(from, to));
    }

    [Fact]
    public void LoadCatalog_ReadsContentInFileOrder()
    {
        var catalog = ReaderFor(ApiFactory.TestContentPath).LoadCatalog();

        Assert.Equal(["alpha", "beta"], catalog.Projects.Select(p => p.Slug));
        Assert.Equal([0, 1], catalog.Projects.Select(p => p.Position));
        Assert.Equal(["PostgreSQL", "C#"], catalog.Projects[0].Technologies.Select(t => t.Name));
        Assert.Equal(["C#", "PostgreSQL", "AWS"], catalog.Technologies.Select(t => t.Name));
        Assert.Equal(["Curso A", "Curso B"], catalog.Education.Select(e => e.Title));
    }

    [Fact]
    public void LoadCatalog_UnknownTechnology_ThrowsNamingProjectAndTechnology()
    {
        var directory = CopyOfTestContent();
        Replace(directory, "projects.json", "\"C#\"", "\"Cobol\"");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains("alpha", exception.Message);
        Assert.Contains("Cobol", exception.Message);
    }

    [Fact]
    public void LoadCatalog_DuplicateSlug_ThrowsNamingSlug()
    {
        var directory = CopyOfTestContent();
        Replace(directory, "projects.json", "\"slug\": \"beta\"", "\"slug\": \"alpha\"");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains("Slug repetido", exception.Message);
        Assert.Contains("alpha", exception.Message);
    }

    [Fact]
    public void LoadCatalog_MissingFile_ThrowsNamingFile()
    {
        var directory = CopyOfTestContent();
        File.Delete(Path.Combine(directory, "education.json"));

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains("education.json", exception.Message);
    }

    [Fact]
    public void LoadCatalog_InvalidJson_ThrowsNamingFile()
    {
        var directory = CopyOfTestContent();
        File.WriteAllText(Path.Combine(directory, "technologies.json"), "{ isto não é json");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains("technologies.json", exception.Message);
    }

    [Fact]
    public void LoadCatalog_MissingRequiredField_ThrowsNamingFile()
    {
        var directory = CopyOfTestContent();
        Replace(directory, "education.json", "\"title\": \"Curso A\", ", "");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains("education.json", exception.Message);
        Assert.Contains("title", exception.Message);
    }

    [Theory]
    // enum como número: passaria como valor que não existe
    [InlineData("projects.json", "\"status\": \"Done\"", "\"status\": 7", "projects.json")]
    // campo que o formato não conhece (erro de digitação)
    [InlineData("projects.json", "\"featured\": false,", "\"featured\": false, \"extra\": 1,", "projects.json")]
    // slug vira URL: só minúsculas, números e hífen
    [InlineData("projects.json", "\"slug\": \"beta\"", "\"slug\": \"Beta Dois\"", "Beta Dois")]
    // null dentro das listas
    [InlineData("projects.json", "\"highlights\": []", "\"highlights\": [null]", "beta")]
    [InlineData("projects.json", "[\"PostgreSQL\"]", "[null]", "beta")]
    // links: só http(s) absoluto
    [InlineData("projects.json", "\"https://example.com/alpha\"", "\"example.com/alpha\"", "example.com/alpha")]
    [InlineData("projects.json", "\"https://example.com/beta\"", "\"javascript:alert(1)\"", "javascript:alert(1)")]
    public void LoadCatalog_InvalidContent_ThrowsNamingTheProblem(
        string fileName, string from, string to, string expectedInMessage)
    {
        var directory = CopyOfTestContent();
        Replace(directory, fileName, from, to);

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains(expectedInMessage, exception.Message);
    }

    [Theory]
    [InlineData("technologies.json")]
    [InlineData("projects.json")]
    [InlineData("education.json")]
    public void LoadCatalog_EmptyList_ThrowsNamingFile(string fileName)
    {
        // Lista vazia faria a sincronização apagar o conteúdo do site sem erro nenhum.
        var directory = CopyOfTestContent();
        File.WriteAllText(Path.Combine(directory, fileName), "[]");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadCatalog());

        Assert.Contains(fileName, exception.Message);
    }

    [Fact]
    public void RealContent_Loads()
    {
        // Caminho padrão: o content/ real do repositório, copiado para a saída do build.
        var reader = ReaderFor(new ContentOptions().Path);
        var catalog = reader.LoadCatalog();

        Assert.NotEmpty(catalog.Projects);
        Assert.NotEmpty(catalog.Technologies);
        Assert.NotEmpty(catalog.Education);
        Assert.NotEmpty(reader.LoadProfile().ShortName);
    }

    [Fact]
    public void LoadProfile_InvalidLink_ThrowsNamingFile()
    {
        var directory = CopyOfTestContent();
        Replace(directory, "profile.json", "\"https://example.com/github\"", "\"github.com/alguem\"");

        var exception = Assert.Throws<InvalidOperationException>(() => ReaderFor(directory).LoadProfile());

        Assert.Contains("profile.json", exception.Message);
        Assert.Contains("github.com/alguem", exception.Message);
    }

    /// <summary>Sincroniza com o conteúdo de teste, como a API faz na subida.</summary>
    private async Task SyncTestContent()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SyncAsync();
    }

    private async Task AssertDatabaseHasTestContent()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        Assert.Equal(2, await db.Projects.CountAsync());
        Assert.Equal(3, await db.Technologies.CountAsync());
        Assert.Equal(2, await db.EducationItems.CountAsync());
        Assert.Equal(3, await db.Projects.SelectMany(p => p.Technologies).CountAsync());
    }

    [Fact]
    public async Task Sync_Twice_KeepsSameRows()
    {
        await SyncTestContent();
        await SyncTestContent();

        await AssertDatabaseHasTestContent();
    }

    [Fact]
    public async Task Sync_Concurrent_DoesNotCollide()
    {
        // Deploy sem downtime: a instância nova sobe (e sincroniza) com a antiga ainda no ar.
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => SyncTestContent()));

        await AssertDatabaseHasTestContent();
    }

    [Fact]
    public async Task Sync_InvalidContent_LeavesDatabaseUntouched()
    {
        var directory = CopyOfTestContent();
        Replace(directory, "projects.json", "\"C#\"", "\"Cobol\"");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            var seeder = new ContentSeeder(db, ReaderFor(directory));

            await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SyncAsync());
        }

        await AssertDatabaseHasTestContent();
    }

    [Fact]
    public async Task Sync_RealContent_ReplacesOldRowsAndFitsTheDatabase()
    {
        // Passa o content/ real pelo banco de verdade: pega o que a validação em memória
        // não vê (tamanho de coluna, índice único) antes de chegar ao deploy.
        var realContent = ReaderFor(new ContentOptions().Path);

        try
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            await new ContentSeeder(db, realContent).SyncAsync();

            var slugs = await db.Projects.OrderBy(p => p.Position).Select(p => p.Slug).ToListAsync();
            Assert.Equal(realContent.LoadCatalog().Projects.Select(p => p.Slug), slugs);
            Assert.DoesNotContain("alpha", slugs);
        }
        finally
        {
            // Os outros testes da coleção contam com o conteúdo de teste.
            await SyncTestContent();
        }
    }
}
