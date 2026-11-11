using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/technologies")]
public class TechnologiesController(PortfolioDbContext db) : ControllerBase
{
    /// <summary>Tecnologias com os projetos que as usam. É daí que sai o "comprovada".</summary>
    [HttpGet]
    public async Task<List<TechnologyResponse>> GetAll(CancellationToken cancellationToken) =>
        await db.Technologies
            .AsNoTracking()
            .OrderBy(t => t.Position)
            .Select(t => new TechnologyResponse(
                t.Name,
                t.Category,
                t.Studying,
                t.Projects
                    .OrderBy(p => p.Position)
                    .Select(p => new ProjectReference(p.Slug, p.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);
}
