using System.Security.Claims;
using EventosPeruIA.CORE.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosPeruIA.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EntradasController : ControllerBase
    {
        private readonly IEntradaService _entradaService;

        public EntradasController(IEntradaService entradaService)
        {
            _entradaService = entradaService;
        }

        [HttpGet("mis-entradas")]
        public async Task<IActionResult> GetMisEntradas()
        {
            int userId = GetUserIdFromClaims();
            var entradas = await _entradaService.GetMisEntradasAsync(userId);
            return Ok(entradas);
        }

        [HttpGet("{id:guid}/voucher")]
        public async Task<IActionResult> GetVoucher(Guid id)
        {
            try
            {
                int userId = GetUserIdFromClaims();
                var voucher = await _entradaService.GetVoucherByEntradaIdAsync(id, userId);
                return Ok(voucher);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        private int GetUserIdFromClaims()
        {
            var claim = User.FindFirst("UserId")
                        ?? User.FindFirst("UsuarioId")
                        ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");
            }
            return userId;
        }
    }
}