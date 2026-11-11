using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks().AddDbContextCheck<PortfolioDbContext>();

builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection("Content"));
builder.Services.AddSingleton<ContentReader>();
builder.Services.AddScoped<ContentSeeder>();
builder.Services.AddSingleton(services =>
    services.GetRequiredService<ContentReader>().Read<ProfileResponse>("profile.json"));

var app = builder.Build();

// Simplificação consciente: migrations e conteúdo são aplicados na subida. Funciona com
// uma instância só; com mais de uma, isso vira um passo do deploy.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SyncAsync();

    // Lê o perfil agora: profile.json com erro derruba a subida, não a primeira requisição.
    app.Services.GetRequiredService<ProfileResponse>();
}

// Erros e 404 saem como application/problem+json.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
