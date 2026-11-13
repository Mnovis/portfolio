using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Contracts;

namespace Portfolio.Api.Services;

/// <summary>
/// Configuração do painel (seção Admin). Em produção vem de variáveis de ambiente
/// (Admin__Email, Admin__Password, Admin__JwtKey); nada disso fica no repositório.
/// </summary>
public class AdminOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }

    /// <summary>Segredo que assina os tokens. Quem tem a chave consegue fabricar um token válido.</summary>
    public string? JwtKey { get; set; }
}

/// <summary>Confere o login do painel e emite o token (JWT) que as outras rotas exigem.</summary>
// ponytail: um administrador só, com e-mail e senha na configuração. Com um segundo
// usuário, isso vira tabela de usuários com hash de senha (PasswordHasher).
public class AdminAuth(AdminOptions options)
{
    public const string Issuer = "portfolio-api";

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);

    /// <summary>A mesma chave assina o token no login e confere a assinatura a cada requisição.</summary>
    public SymmetricSecurityKey SigningKey { get; } = ReadKey(options.JwtKey);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Email)
        && !string.IsNullOrEmpty(options.Password)
        && !string.IsNullOrWhiteSpace(options.JwtKey);

    // "&" em vez de "&&": as duas comparações sempre rodam, então o tempo de resposta
    // não revela se foi o e-mail ou a senha que errou.
    public bool CredentialsMatch(string email, string password) =>
        SameText(email.Trim().ToLowerInvariant(), (options.Email ?? "").Trim().ToLowerInvariant())
        & SameText(password, options.Password ?? "");

    public LoginResponse CreateToken()
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Issuer,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, options.Email!)]),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });

        return new LoginResponse(token, expiresAt);
    }

    /// <summary>
    /// Compara em tempo constante. Um "==" comum para no primeiro caractere diferente, e
    /// medindo o tempo dá para descobrir a senha aos poucos. O SHA-256 iguala os tamanhos,
    /// que é o que FixedTimeEquals exige.
    /// </summary>
    private static bool SameText(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));

    private static SymmetricSecurityKey ReadKey(string? configured)
    {
        // Sem chave o login já responde 503; a chave aleatória só mantém a validação de pé
        // (nenhum token existe para ela validar).
        if (string.IsNullOrWhiteSpace(configured))
            return new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));

        var bytes = Encoding.UTF8.GetBytes(configured.Trim());
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Admin:JwtKey precisa ter pelo menos 32 caracteres: é o mínimo para assinar com HMAC-SHA256.");

        return new SymmetricSecurityKey(bytes);
    }
}
