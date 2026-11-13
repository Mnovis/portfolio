using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.Contracts;

// Entrada e saída do painel de administrador (rotas api/admin).

public class LoginRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(254)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(200)]
    public string Password { get; set; } = "";
}

public record LoginResponse(string Token, DateTimeOffset ExpiresAt);

public record AdminMessageResponse(
    int Id,
    string Name,
    string Email,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NotifiedAt,
    DateTimeOffset? ReadAt);

public class MessageUpdateRequest
{
    // bool? com [Required]: corpo sem "read" vira 400, em vez de virar "false" sem ninguém pedir.
    [Required(ErrorMessage = "Informe read: true ou false.")]
    public bool? Read { get; set; }
}

public record OverviewResponse(
    int Messages,
    int Unread,
    int NotNotified,
    int Projects,
    int Technologies,
    int EducationItems);
