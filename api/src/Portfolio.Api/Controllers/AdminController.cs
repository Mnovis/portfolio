using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;
using Portfolio.Api.Services;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Painel de administrador. [Authorize] na classe fecha todas as rotas: sem token válido
/// a resposta é 401. A única exceção é o login, marcado com [AllowAnonymous].
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController(PortfolioDbContext db, AdminAuth auth, ILogger<AdminController> logger) : ControllerBase
{
    public const string LoginRateLimitPolicy = "login";

    // ponytail: sem paginação; devolve as 200 mais recentes. Paginar quando o volume passar disso.
    private const int MaxMessages = 200;

    #region Login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        if (!auth.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Painel não configurado.");

        if (!auth.CredentialsMatch(request.Email, request.Password))
        {
            logger.LogWarning("Login do painel recusado para o IP {Ip}.", HttpContext.Connection.RemoteIpAddress);

            // A mesma resposta para e-mail errado e para senha errada.
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha incorretos.");
        }

        return auth.CreateToken();
    }
    #endregion

    #region Messages
    [HttpGet("messages")]
    public async Task<List<AdminMessageResponse>> GetMessages(CancellationToken cancellationToken) =>
        await db.ContactMessages
            .AsNoTracking()
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Take(MaxMessages)
            .Select(message => new AdminMessageResponse(
                message.Id, message.Name, message.Email, message.Message,
                message.CreatedAt, message.NotifiedAt, message.ReadAt))
            .ToListAsync(cancellationToken);

    [HttpPatch("messages/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMessage(
        int id, MessageUpdateRequest request, CancellationToken cancellationToken)
    {
        var message = await db.ContactMessages.FindAsync([id], cancellationToken);
        if (message is null)
            return NotFound();

        // Marcar de novo como lida mantém a data da primeira leitura.
        message.ReadAt = request.Read == true ? message.ReadAt ?? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("messages/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(int id, CancellationToken cancellationToken)
    {
        // ExecuteDeleteAsync manda um DELETE direto, sem carregar a mensagem antes.
        var deleted = await db.ContactMessages
            .Where(message => message.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? NotFound() : NoContent();
    }
    #endregion

    #region Overview
    [HttpGet("overview")]
    public async Task<OverviewResponse> GetOverview(CancellationToken cancellationToken) => new(
        Messages: await db.ContactMessages.CountAsync(cancellationToken),
        Unread: await db.ContactMessages.CountAsync(message => message.ReadAt == null, cancellationToken),
        NotNotified: await db.ContactMessages.CountAsync(message => message.NotifiedAt == null, cancellationToken),
        Projects: await db.Projects.CountAsync(cancellationToken),
        Technologies: await db.Technologies.CountAsync(cancellationToken),
        EducationItems: await db.EducationItems.CountAsync(cancellationToken));
    #endregion
}
