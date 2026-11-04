using System.Text;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using EventosPeruIA.CORE.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace EventosPeruIA.CORE.Infrastructure.Shared
{
    public static class ServiceRegistration
    {
        public static void AddSharedInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<JWTSettings>(config.GetSection("JWTSettings"));
            services.AddTransient<IJWTService, JWTService>();
            var secret = config["JWTSettings:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
                throw new InvalidOperationException("Configura JWTSettings:SecretKey (mínimo 32 bytes) fuera de Git.");
            if (string.IsNullOrWhiteSpace(config["JWTSettings:Issuer"]) || string.IsNullOrWhiteSpace(config["JWTSettings:Audience"]))
                throw new InvalidOperationException("Configura JWTSettings:Issuer y JWTSettings:Audience.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (!int.TryParse(context.Principal?.FindFirst("UserId")?.Value, out var id))
                        { context.Fail("Identidad inválida."); return; }
                        var db = context.HttpContext.RequestServices.GetRequiredService<PostgresContext>();
                        var user = await db.Usuarios.AsNoTracking().Where(u => u.UsuarioId == id)
                            .Select(u => new { u.Estado, u.EsAdministrador }).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                        if (user == null || user.Estado != "Activo")
                        { context.Fail("La cuenta no está activa."); return; }
                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var role in identity.FindAll(identity.RoleClaimType).ToList()) identity.RemoveClaim(role);
                        identity.AddClaim(new Claim(identity.RoleClaimType, user.EsAdministrador ? "Admin" : "User"));
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = config["JWTSettings:Issuer"],
                    ValidAudience = config["JWTSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });
        }
    }
}
