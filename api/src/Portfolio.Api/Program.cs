using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Contracts;
using Portfolio.Api.Controllers;
using Portfolio.Api.Data;
using Portfolio.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
        // Erros de validação saem com o nome do campo como está no JSON ("email", não "Email").
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// CORS: só o site chama a API pelo navegador: GET para ler, POST no formulário de contato
// e no login, PATCH e DELETE no painel. Authorization é o cabeçalho que leva o token.
// As origens vêm da configuração (Cors:Origins); em produção, de Cors__Origins__0.
var siteOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(siteOrigins)
        .WithMethods("GET", "POST", "PATCH", "DELETE")
        .WithHeaders("Content-Type", "Authorization")));

// Limites por IP a cada 10 minutos: poucas mensagens de contato e poucas tentativas de
// login (é o que segura quem tenta adivinhar a senha). Atrás do proxy da hospedagem o IP
// de quem visita vem no cabeçalho X-Forwarded-For; a variável de ambiente
// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true faz o ASP.NET Core ler esse cabeçalho.
var contactLimit = builder.Configuration.GetValue("Contact:MaxPerWindow", 3);
var loginLimit = builder.Configuration.GetValue("Admin:MaxLoginsPerWindow", 5);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ContactController.RateLimitPolicy, context => PerIp(context, contactLimit));
    options.AddPolicy(AdminController.LoginRateLimitPolicy, context => PerIp(context, loginLimit));
});

// Painel: o login devolve um JWT assinado; as rotas com [Authorize] só aceitam requisições
// que tragam um token com assinatura, emissor e validade corretos.
var adminAuth = new AdminAuth(builder.Configuration.GetSection("Admin").Get<AdminOptions>() ?? new AdminOptions());
builder.Services.AddSingleton(adminAuth);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = AdminAuth.Issuer,
        ValidAudience = AdminAuth.Issuer,
        IssuerSigningKey = adminAuth.SigningKey,
        // O padrão tolera 5 minutos depois do vencimento; aqui, meio minuto.
        ClockSkew = TimeSpan.FromSeconds(30),
    });
builder.Services.AddAuthorization();

// Aviso por e-mail das mensagens de contato. A chave vem de Email__ApiKey.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpClient<ContactNotifier>(client => client.Timeout = TimeSpan.FromSeconds(10));

// Em produção a connection string vem da variável de ambiente ConnectionStrings__Default.
var connectionString = DatabaseConnection.Read(builder.Configuration.GetConnectionString("Default"));

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
app.UseRateLimiter();

// Autenticação lê o token e descobre quem é; autorização decide se pode. Nessa ordem.
app.UseAuthentication();
app.UseAuthorization();

// Documento OpenAPI em /openapi/v1.json e interface navegável em /docs.
app.MapOpenApi();
app.MapScalarApiReference("/docs");

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Um contador por IP, zerado a cada janela de 10 minutos.
static RateLimitPartition<string> PerIp(HttpContext context, int limit) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(10) });
