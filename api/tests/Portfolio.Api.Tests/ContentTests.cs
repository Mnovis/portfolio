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
    }

    [Fact]
    public void RealContent_Loads()
    {
        // Caminho padrão: o content/ real do repositório, copiado para a saída do build.
        var catalog = ReaderFor(new ContentOptions().Path).LoadCatalog();

        Assert.NotEmpty(catalog.Projects);
        Assert.NotEmpty(catalog.Technologies);
        Assert.NotEmpty(catalog.Education);
    }

    [Fact]
    public async Task Sync_Twice_KeepsSameRows()
    {
        for (var run = 0; run < 2; run++)
        {
            using var syncScope = factory.Services.CreateScope();
            await syncScope.ServiceProvider.GetRequiredService<ContentSeeder>().SyncAsync();
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        Assert.Equal(2, await db.Projects.CountAsync());
        Assert.Equal(3, await db.Technologies.CountAsync());
        Assert.Equal(2, await db.EducationItems.CountAsync());
        Assert.Equal(3, await db.Projects.SelectMany(p => p.Technologies).CountAsync());
    }
}
