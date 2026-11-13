using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Portfolio.Api.Domain;

namespace Portfolio.Api.Services;

public class EmailOptions
{
    /// <summary>Chave da API do Resend. Vem da variável de ambiente Email__ApiKey; nunca fica no repositório.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Endereço que recebe os avisos.</summary>
    public string? To { get; set; }

    /// <summary>Remetente. O padrão é o endereço de testes do Resend; com domínio verificado, troque.</summary>
    public string From { get; set; } = "Portfólio <onboarding@resend.dev>";
}

/// <summary>Avisa o dono do site, por e-mail, que chegou uma mensagem de contato.</summary>
public class ContactNotifier(HttpClient http, IOptions<EmailOptions> options, ILogger<ContactNotifier> logger)
{
    /// <summary>
    /// Devolve false quando o envio não está configurado. Lança exceção se o provedor
    /// recusar ou não responder.
    /// </summary>
    public async Task<bool> NotifyAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        var email = options.Value;
        if (string.IsNullOrWhiteSpace(email.ApiKey) || string.IsNullOrWhiteSpace(email.To))
        {
            logger.LogWarning(
                "Envio de e-mail não configurado (Email:ApiKey e Email:To). A mensagem {Id} ficou só no banco.",
                message.Id);
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", email.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = email.From,
            to = new[] { email.To },
            // Responder ao aviso responde direto para quem escreveu.
            reply_to = message.Email,
            subject = $"Contato pelo portfólio: {message.Name}",
            // Texto puro: o que a pessoa escreveu nunca é interpretado como HTML.
            text = $"{message.Name} <{message.Email}> escreveu em {message.CreatedAt:dd/MM/yyyy HH:mm} (UTC):\n\n{message.Message}",
        });

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // O corpo da resposta diz o motivo da recusa (remetente não verificado,
            // destinatário não permitido, chave inválida). Sem ele o log só teria o código HTTP.
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"O provedor de e-mail respondeu {(int)response.StatusCode}: {reason}");
        }

        return true;
    }
}
