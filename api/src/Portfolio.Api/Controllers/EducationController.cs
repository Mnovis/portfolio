using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/education")]
public class EducationController(PortfolioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<EducationItemResponse>> GetAll(CancellationToken cancellationToken) =>
        await db.EducationItems
            .AsNoTracking()
            .OrderBy(e => e.Position)
            .Select(e => new EducationItemResponse(e.Institution, e.Title, e.Detail, e.Status))
            .ToListAsync(cancellationToken);
}
