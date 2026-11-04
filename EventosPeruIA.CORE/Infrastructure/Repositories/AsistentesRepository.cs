using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public sealed class AsistentesRepository(PostgresContext db) : IAsistentesRepository
{
    public Task<Eventos?> Evento(int id, CancellationToken ct) => db.Eventos.AsNoTracking().SingleOrDefaultAsync(e => e.EventoId == id, ct);
    public async Task<AsistentesResponseDTO> Listar(int eventoId, string? buscar, string? estado, int pagina, int tamano, CancellationToken ct)
    {
        var entradas = db.Entradas.AsNoTracking().Where(e => e.EventoId == eventoId);
        var validas = await entradas.CountAsync(e => e.Estado == "Valida", ct);
        var utilizadas = await entradas.CountAsync(e => e.Estado == "Utilizada", ct);
        var anuladas = await entradas.CountAsync(e => e.Estado == "Anulada", ct);
        var aprobados = db.Checkins.Where(c => c.EventoId == eventoId && c.Resultado == "Aprobado");
        var checkins = await entradas.CountAsync(e => aprobados.Any(c => c.EntradaId == e.EntradaId), ct);
        var query = entradas;
        if (estado != null) query = query.Where(e => e.Estado == estado);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var text = buscar.Trim();
            if (Guid.TryParse(text, out var uuid)) query = query.Where(e => e.EntradaId == uuid);
            else query = query.Where(e => e.CodigoCorto == text);
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(e => e.FechaEmision).ThenBy(e => e.EntradaId).Skip((pagina - 1) * tamano).Take(tamano)
            .Select(e => new AsistenteDTO(e.EntradaId, e.CodigoCorto, e.CompradorId, e.Comprador.Nombres + " " + e.Comprador.Apellidos,
                e.Comprador.Correo, e.Estado, aprobados.Any(c => c.EntradaId == e.EntradaId),
                aprobados.Where(c => c.EntradaId == e.EntradaId).OrderBy(c => c.FechaHora).Select(c => (DateTime?)c.FechaHora).FirstOrDefault()))
            .ToListAsync(ct);
        return new(eventoId, validas + utilizadas, checkins, validas, utilizadas, anuladas, total, pagina, tamano, rows);
    }
}
