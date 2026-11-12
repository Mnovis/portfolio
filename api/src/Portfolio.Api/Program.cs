using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Contracts;
using Portfolio.Api.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// CORS: só o site chama a API pelo navegador, e só para ler. As origens vêm da
// configuração (Cors:Origins); em produção, da variável de ambiente Cors__Origins__0.
var siteOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(siteOrigins).WithMethods("GET")));

// Em produção a connection string vem da variável de ambiente ConnectionStrings__Default.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não está configurada.");

builder.Services.AddDbContext<PortfolioDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks().AddDbContextCheck<PortfolioDbContext>();

builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection("Content"));
builder.Services.AddSingleton<ContentReader>();
builder.Services.AddScoped<ContentSeeder>();
builder.Services.AddSingleton(services => services.GetRequiredService<ContentReader>().LoadProfile());

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

app.UseCors();

// Documento OpenAPI em /openapi/v1.json e interface navegável em /docs.
app.MapOpenApi();
app.MapScalarApiReference("/docs");

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
