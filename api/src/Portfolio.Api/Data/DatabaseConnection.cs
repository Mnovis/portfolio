using Npgsql;

namespace Portfolio.Api.Data;

public static class DatabaseConnection
{
    private const string ExpectedFormat = "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require";

    /// <summary>
    /// Confere a connection string na subida, antes de qualquer conexão. Em produção ela é
    /// colada no painel da hospedagem, e um valor errado tem que dar um erro que diga o que
    /// fazer. As mensagens daqui nunca repetem o valor: ele tem a senha do banco.
    /// </summary>
    public static string Read(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("ConnectionStrings:Default não está configurada.");

        // Aspas e espaços costumam vir junto quando o valor é copiado de um exemplo de código.
        var connectionString = configured.Trim().Trim('"', '\'').Trim();

        if (connectionString.Contains("://"))
            throw new InvalidOperationException(
                "ConnectionStrings:Default está no formato de endereço (postgresql://...). " +
                $"Use o formato .NET: {ExpectedFormat}");

        try
        {
            // Só confere o formato; não abre conexão.
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // A exceção original pode citar um pedaço do valor; por isso ela não segue adiante.
            throw new InvalidOperationException(
                $"ConnectionStrings:Default está em formato inválido. Formato esperado: {ExpectedFormat}");
        }

        return connectionString;
    }
}
