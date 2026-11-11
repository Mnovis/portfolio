namespace Portfolio.Api.Domain;

public enum EducationStatus
{
    Done,
    InProgress
}

public class EducationItem
{
    public int Id { get; set; }
    public required string Institution { get; set; }
    public required string Title { get; set; }
    public string? Detail { get; set; }
    public EducationStatus Status { get; set; }
    public int Position { get; set; }
}
