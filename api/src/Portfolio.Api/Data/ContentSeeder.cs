using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Data;

/// <summary>
/// Deixa o banco igual ao content/. Os arquivos são a fonte de verdade; as tabelas de
/// conteúdo são derivadas, então a sincronização apaga e reinsere tudo.
/// </summary>
public class ContentSeeder(PortfolioDbContext db, ContentReader reader)
{
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        // Valida antes de tocar o banco: conteúdo com erro não apaga o que já está lá.
        var catalog = reader.LoadCatalog();

        // Uma transação: quem consulta nunca vê o banco pela metade.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Projects.ExecuteDeleteAsync(cancellationToken);
        await db.Technologies.ExecuteDeleteAsync(cancellationToken);
        await db.EducationItems.ExecuteDeleteAsync(cancellationToken);

        db.AddRange(catalog.Technologies);
        db.AddRange(catalog.Projects);
        db.AddRange(catalog.Education);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
