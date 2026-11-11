namespace Portfolio.Api.Domain;

public enum TechnologyCategory
{
    Backend,
    Data,
    Auth,
    Realtime,
    Testing,
    Infra,
    Frontend
}

public class Technology
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TechnologyCategory Category { get; set; }

    /// <summary>Marcado à mão: tecnologia que o Mateus está estudando agora.</summary>
    public bool Studying { get; set; }

    public int Position { get; set; }

    /// <summary>Projetos que usam a tecnologia. Ter pelo menos um é o que a torna "comprovada".</summary>
    public List<Project> Projects { get; set; } = [];
}
