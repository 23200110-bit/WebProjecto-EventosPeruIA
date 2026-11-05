using EventosPeruIA.CORE.Infrastructure.Shared;
using EventosPeruIA.CORE.Infrastructure.Data;
using EventosPeruIA.CORE.Core.Services;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using EventosPeruIA.CORE.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using EventosPeruIA.CORE.Core.DTOs;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var _config = builder.Configuration;
var cnx = _config.GetConnectionString("DevConnection");

if (string.IsNullOrWhiteSpace(cnx))
	throw new InvalidOperationException("Configura ConnectionStrings__DevConnection como variable de entorno o ConnectionStrings:DevConnection con user-secrets; no guardes contraseñas en appsettings.json.");

builder.Services.AddDbContext<PostgresContext>(options =>
	options.UseNpgsql(cnx));

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IEventReviewRepository, EventReviewRepository>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IEventReviewService, EventReviewService>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IEventoService, EventoService>();

builder.Services.AddScoped<IPersonalRepository, PersonalRepository>();
builder.Services.AddScoped<IPersonalService, PersonalService>();
builder.Services.AddScoped<IEventCatalogRepository, EventCatalogRepository>();
builder.Services.AddScoped<IEventCatalogService, EventCatalogService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

// Registros de Santiago (US-012)
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<ICheckInService, CheckInService>();

builder.Services.AddSharedInfrastructure(_config);
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAsistentesRepository, AsistentesRepository>();
builder.Services.AddScoped<IAsistentesService, AsistentesService>();
builder.Services.AddScoped<IAiRepository, AiRepository>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton(TimeProvider.System);
var proveedorIA = _config["AI:Provider"] ?? "Gemini";
if (proveedorIA.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IModeloIA, GeminiModeloIA>(client => client.Timeout = TimeSpan.FromMinutes(2));
else if (proveedorIA.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IModeloIA, OllamaModeloIA>(client =>
{
    client.BaseAddress = new Uri(_config["AI:Ollama:BaseUrl"] ?? "http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(3);
});
else throw new InvalidOperationException("AI:Provider debe ser Gemini u Ollama.");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("UserId")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (ProyectoException exception)
    {
        context.Response.StatusCode = exception.Status;
        await context.Response.WriteAsJsonAsync(new { mensaje = exception.Message });
    }
    catch (Npgsql.PostgresException exception) when (context.Request.Path.StartsWithSegments("/api/admin") && exception.SqlState is "P0001" or "23514")
    {
        context.Response.StatusCode = 409;
        await context.Response.WriteAsJsonAsync(new { mensaje = "La acción no es válida para el estado actual del registro." });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }
