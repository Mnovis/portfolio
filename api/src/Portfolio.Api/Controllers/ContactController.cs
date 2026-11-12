using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;
using Portfolio.Api.Domain;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController(
    PortfolioDbContext db,
    ContactNotifier notifier,
    ILogger<ContactController> logger) : ControllerBase
{
    public const string RateLimitPolicy = "contact";

    #region Send
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Send(ContactRequest request, CancellationToken cancellationToken)
    {
        // Campo-isca preenchido = robô. Responde como se tivesse dado certo, para ele não insistir.
        if (!string.IsNullOrEmpty(request.Website))
            return Accepted();

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.ContactMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        // Primeiro grava, depois avisa: se o e-mail falhar, a mensagem já está no banco.
        try
        {
            if (await notifier.NotifyAsync(message, cancellationToken))
            {
                message.NotifiedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Qualquer falha do provedor de e-mail fica no log; quem escreveu não tem culpa dela.
            logger.LogError(exception, "Falha ao avisar por e-mail sobre a mensagem {Id}.", message.Id);
        }

        return Accepted();
    }
    #endregion
}
