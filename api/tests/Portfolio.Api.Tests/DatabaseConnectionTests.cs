using Portfolio.Api.Data;

namespace Portfolio.Api.Tests;

/// <summary>Não precisa de banco: só confere como a connection string é lida na subida.</summary>
public class DatabaseConnectionTests
{
    private const string Valid = "Host=db.exemplo.test;Database=app;Username=u;Password=segredo123;SSL Mode=Require";

    [Theory]
    [InlineData(Valid)]
    [InlineData("\"" + Valid + "\"")]
    [InlineData("'" + Valid + "'")]
    [InlineData(Valid + "\"")]
    [InlineData("  " + Valid + " \n")]
    public void Read_StripsQuotesAndSpacesLeftByCopyAndPaste(string pasted)
    {
        Assert.Equal(Valid, DatabaseConnection.Read(pasted));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Read_Missing_ThrowsSayingItIsNotConfigured(string? value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Read(value));

        Assert.Contains("não está configurada", exception.Message);
    }

    [Theory]
    // endereço no formato de URI, como alguns painéis entregam
    [InlineData("postgresql://u:segredo123@db.exemplo.test/app?sslmode=require")]
    // sobra de texto depois do último par
    [InlineData(Valid + ";isto não é um par")]
    // palavra-chave que não existe
    [InlineData(Valid + ";Servidor=outro")]
    public void Read_Invalid_ExplainsTheFormatWithoutLeakingTheValue(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Read(value));

        Assert.Contains("Host=", exception.Message);
        // A connection string tem a senha do banco: nada dela pode ir para o log.
        Assert.DoesNotContain("segredo123", exception.ToString());
        Assert.DoesNotContain("db.exemplo.test", exception.ToString());
    }
}
