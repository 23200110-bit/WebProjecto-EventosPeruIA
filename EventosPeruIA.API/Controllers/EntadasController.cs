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
            int userId = GetUserIdFromClaims();
            var voucher = await _entradaService.GetVoucherByEntradaIdAsync(id, userId);
            return Ok(voucher);
        }

        private int GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o token inválido.");
            }
            return userId;
        }
    }
}