namespace Portfolio.Api.Domain;

/// <summary>Mensagem enviada pelo formulário de contato do site.</summary>
public class ContactMessage
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Quando o aviso por e-mail saiu. Null = gravada, mas o aviso ainda não foi enviado.</summary>
    public DateTimeOffset? NotifiedAt { get; set; }
}
