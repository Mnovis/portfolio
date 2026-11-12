using System.ComponentModel.DataAnnotations;

namespace Portfolio.Api.Contracts;

/// <summary>
/// Corpo de POST /api/contact. Vem de fora, então tudo é validado aqui: com
/// [ApiController], uma requisição inválida vira 400 antes de chegar ao controller.
/// </summary>
public class ContactRequest
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome precisa ter entre 2 e 100 caracteres.")]
    [RegularExpression(@"^[^\p{Cc}]+$", ErrorMessage = "O nome tem caracteres inválidos.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [StringLength(254, ErrorMessage = "O e-mail é longo demais.")]
    [RegularExpression(@"^\S+@\S+\.\S+$", ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escreva a mensagem.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "A mensagem precisa ter entre 10 e 4000 caracteres.")]
    public string Message { get; set; } = "";

    /// <summary>
    /// Campo-isca: existe no formulário, mas fica escondido. Gente não preenche; robô
    /// que preenche tudo, sim.
    /// </summary>
    public string? Website { get; set; }
}
