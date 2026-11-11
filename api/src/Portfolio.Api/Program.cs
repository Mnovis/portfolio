using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks().AddDbContextCheck<PortfolioDbContext>();

var app = builder.Build();

// Simplificação consciente: as migrations são aplicadas na subida. Funciona com uma
// instância só; com mais de uma, isso vira um passo do deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await db.Database.MigrateAsync();
}

// Erros e 404 saem como application/problem+json.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
