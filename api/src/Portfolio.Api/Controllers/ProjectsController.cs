using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(PortfolioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<ProjectSummaryResponse>> GetAll(CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Position)
            .Select(p => new ProjectSummaryResponse(
                p.Slug,
                p.Name,
                p.Summary,
                p.Status,
                p.Featured,
                p.Technologies.OrderBy(t => t.Position).Select(t => t.Name).ToList()))
            .ToListAsync(cancellationToken);

    [HttpGet("{slug}")]
    [ProducesResponseType<ProjectDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => new ProjectDetailResponse(
                p.Slug,
                p.Name,
                p.Summary,
                p.Context,
                p.Highlights,
                p.Status,
                p.Featured,
                p.RepoUrl,
                p.LiveUrl,
                p.Technologies.OrderBy(t => t.Position).Select(t => t.Name).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return project is null ? NotFound() : project;
    }
}
