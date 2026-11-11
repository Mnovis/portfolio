using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Data;

/// <summary>
/// Deixa o banco igual ao content/. Os arquivos são a fonte de verdade; as tabelas de
/// conteúdo são derivadas, então a sincronização apaga e reinsere tudo.
/// </summary>
public class ContentSeeder(PortfolioDbContext db, ContentReader reader)
{
    // Número qualquer, fixo: identifica "sincronização de conteúdo" entre as travas do banco.
    private const long SyncLockKey = 827364;

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        // Valida antes de tocar o banco: conteúdo com erro não apaga o que já está lá.
        var catalog = reader.LoadCatalog();

        // Uma transação: quem consulta nunca vê o banco pela metade.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Num deploy sem downtime a instância nova sobe com a antiga ainda no ar. Duas
        // sincronizações ao mesmo tempo bateriam no índice único. Esta trava é do
        // PostgreSQL: a segunda espera a primeira, e ela solta sozinha no fim da transação.
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SyncLockKey})", cancellationToken);

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
