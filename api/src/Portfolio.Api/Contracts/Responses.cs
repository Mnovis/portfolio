using Portfolio.Api.Domain;

namespace Portfolio.Api.Contracts;

// DTOs de saída: o que a API devolve. As entidades do banco nunca saem direto.

public record ProfileResponse(
    string Name,
    string ShortName,
    string Headline,
    string Focus,
    string Summary,
    string Goal,
    string Location,
    bool Remote,
    ProfileLinks Links);

public record ProfileLinks(string Github, string Linkedin);

public record ProjectSummaryResponse(
    string Slug,
    string Name,
    string Summary,
    ProjectStatus Status,
    bool Featured,
    List<string> Technologies);

public record ProjectDetailResponse(
    string Slug,
    string Name,
    string Summary,
    string? Context,
    List<string> Highlights,
    ProjectStatus Status,
    bool Featured,
    string? RepoUrl,
    string? LiveUrl,
    List<string> Technologies);

public record EducationItemResponse(string Institution, string Title, string? Detail, EducationStatus Status);

public record ProjectReference(string Slug, string Name);

public record TechnologyResponse(
    string Name,
    TechnologyCategory Category,
    bool Studying,
    List<ProjectReference> Projects)
{
    /// <summary>Comprovada = usada em pelo menos um projeto. Não é digitado em lugar nenhum.</summary>
    public bool Proven => Projects.Count > 0;
}
