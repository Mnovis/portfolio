namespace Portfolio.Api.Domain;

public enum ProjectStatus
{
    Done,
    InProgress
}

public class Project
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string Summary { get; set; }
    public string? Context { get; set; }
    public List<string> Highlights { get; set; } = [];
    public ProjectStatus Status { get; set; }
    public bool Featured { get; set; }
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }

    /// <summary>Posição no arquivo de conteúdo; define a ordem de exibição.</summary>
    public int Position { get; set; }

    public List<Technology> Technologies { get; set; } = [];
}
