using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Auth;
using EventosPeruIA.CORE.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
namespace EventosPeruIA.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _service;
        private readonly IJWTService _jwtService;
        public AuthController(IAccountService service, IJWTService jwtService)
        {
            _service = service;
            _jwtService = jwtService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var user = await _service.RegisterAsync(request);
                return StatusCode(201, user);
            }
            catch (ValidationException exception) { return BadRequest(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _service.LoginAsync(request);
            if (user == null) return Unauthorized(new { message = "Correo o contraseña incorrectos" });
            return Ok(new LoginResponse { AccessToken = _jwtService.GenerateJWToken(user), User = user });
        }

        [HttpPost("google")]
        public async Task<IActionResult> Google(GoogleLoginRequest request, [FromServices] IGoogleAuthService google, CancellationToken ct)
        {
            var user = await google.LoginAsync(request, ct);
            if (user == null) return Unauthorized(new { message = "No se pudo iniciar sesión con Google" });
            return Ok(new LoginResponse { AccessToken = _jwtService.GenerateJWToken(user), User = user });
        }
    }
}
