using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs.Auth;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using EventosPeruIA.CORE.Core.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace EventosPeruIA.CORE.Infrastructure.Shared
{
    public class JWTService : IJWTService
    {
        private readonly JWTSettings _settings;
        public JWTService(IOptions<JWTSettings> settings)
        {
            _settings = settings.Value;
        }
        public string GenerateJWToken(AccountResponse user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Role, user.IsAdministrator ? "Admin" : "User")
            };
            var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience,
                claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(_settings.ExpirationInMinutes), credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
