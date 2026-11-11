using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Portfolio.Api.Contracts;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Data;

public class ContentOptions
{
    /// <summary>Pasta com os arquivos JSON de conteúdo.</summary>
    public string Path { get; set; } = System.IO.Path.Combine(AppContext.BaseDirectory, "content");
}

/// <summary>Conteúdo já validado, pronto para ir ao banco.</summary>
public record Catalog(List<Technology> Technologies, List<Project> Projects, List<EducationItem> Education);

/// <summary>
/// Lê os arquivos de content/ e valida tudo em memória. Qualquer problema vira uma
/// exceção com mensagem que diz qual arquivo e qual item estão errados.
/// </summary>
public class ContentReader(IOptions<ContentOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Enum só por nome: "status": 7 seria aceito como um valor que não existe.
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        // Campo ausente, null onde não pode ou nome digitado errado: erro, em vez de
        // valor padrão silencioso.
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    // O slug vira URL: minúsculas, números e hífen.
    private static readonly Regex SlugFormat = new("^[a-z0-9]+(-[a-z0-9]+)*$");

    // Formato dos arquivos. Separado das entidades para o arquivo não depender do banco.
    private record TechnologyFile(string Name, TechnologyCategory Category, bool Studying);

    private record ProjectFile(
        string Slug, string Name, string Summary, string? Context, List<string> Highlights,
        ProjectStatus Status, bool Featured, string? RepoUrl, string? LiveUrl, List<string> Technologies);

    private record EducationFile(string Institution, string Title, string? Detail, EducationStatus Status);

    public ProfileResponse LoadProfile()
    {
        var profile = Read<ProfileResponse>("profile.json");
        RequireHttpUrl(profile.Links.Github, "profile.json (links.github)");
        RequireHttpUrl(profile.Links.Linkedin, "profile.json (links.linkedin)");
        return profile;
    }

    public Catalog LoadCatalog()
    {
        var technologies = new Dictionary<string, Technology>();
        foreach (var (position, file) in ReadList<TechnologyFile>("technologies.json").Index())
        {
            var technology = new Technology
            {
                Name = file.Name,
                Category = file.Category,
                Studying = file.Studying,
                Position = position,
            };

            if (!technologies.TryAdd(file.Name, technology))
                throw new InvalidOperationException($"Tecnologia repetida em technologies.json: '{file.Name}'.");
        }

        var projects = new List<Project>();
        var slugs = new HashSet<string>();
        foreach (var (position, file) in ReadList<ProjectFile>("projects.json").Index())
        {
            if (!SlugFormat.IsMatch(file.Slug))
                throw new InvalidOperationException(
                    $"Slug inválido em projects.json: '{file.Slug}'. Use minúsculas, números e hífen.");

            if (!slugs.Add(file.Slug))
                throw new InvalidOperationException($"Slug repetido em projects.json: '{file.Slug}'.");

            // A checagem de null do JSON não alcança os itens de dentro das listas.
            if (file.Highlights.Any(string.IsNullOrWhiteSpace) || file.Technologies.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException(
                    $"O projeto '{file.Slug}' tem item vazio em highlights ou technologies.");

            RequireHttpUrl(file.RepoUrl, $"projects.json ('{file.Slug}', repoUrl)");
            RequireHttpUrl(file.LiveUrl, $"projects.json ('{file.Slug}', liveUrl)");

            var project = new Project
            {
                Slug = file.Slug,
                Name = file.Name,
                Summary = file.Summary,
                Context = file.Context,
                Highlights = file.Highlights,
                Status = file.Status,
                Featured = file.Featured,
                RepoUrl = file.RepoUrl,
                LiveUrl = file.LiveUrl,
                Position = position,
            };

            foreach (var name in file.Technologies)
            {
                if (!technologies.TryGetValue(name, out var technology))
                    throw new InvalidOperationException(
                        $"O projeto '{file.Slug}' cita a tecnologia '{name}', que não existe em technologies.json.");

                if (project.Technologies.Contains(technology))
                    throw new InvalidOperationException(
                        $"O projeto '{file.Slug}' cita a tecnologia '{name}' mais de uma vez.");

                project.Technologies.Add(technology);
            }

            projects.Add(project);
        }

        var education = ReadList<EducationFile>("education.json")
            .Index()
            .Select(item => new EducationItem
            {
                Institution = item.Item.Institution,
                Title = item.Item.Title,
                Detail = item.Item.Detail,
                Status = item.Item.Status,
                Position = item.Index,
            })
            .ToList();

        return new Catalog([.. technologies.Values], projects, education);
    }

    private T Read<T>(string fileName)
    {
        var path = Path.Combine(options.Value.Path, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException($"Arquivo de conteúdo não encontrado: {path}");

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
                ?? throw new JsonException("o arquivo contém apenas null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Conteúdo inválido em {fileName}: {ex.Message}", ex);
        }
    }

    private List<T> ReadList<T>(string fileName)
    {
        var items = Read<List<T>>(fileName);

        // Lista vazia faria a sincronização apagar o conteúdo do site sem erro nenhum.
        if (items.Count == 0 || items.Any(item => item is null))
            throw new InvalidOperationException($"{fileName} está vazio ou tem um item null.");

        return items;
    }

    private static void RequireHttpUrl(string? value, string where)
    {
        if (value is null)
            return;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException(
                $"Link inválido em {where}: '{value}'. Precisa começar com http:// ou https://.");
    }
}
