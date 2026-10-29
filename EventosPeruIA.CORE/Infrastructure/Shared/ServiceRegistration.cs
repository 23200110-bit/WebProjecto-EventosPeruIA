using System.Text;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using EventosPeruIA.CORE.Core.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
