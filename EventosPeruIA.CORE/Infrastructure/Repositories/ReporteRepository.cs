using System.Data;
using EventosPeruIA.CORE.Core.DTOs.Reportes;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace EventosPeruIA.CORE.Infrastructure.Repositories;

public class ReporteRepository(PostgresContext context) : IReporteRepository
{
    private static readonly TimeZoneInfo ZonaLima =
        TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

    public async Task<DatosReporteRaw> ObtenerDatosAsync(
        int organizadorId, ReporteFiltroRequestDto filtro)
    {
        var eventoId = filtro.EventoId;
        DateTime? inicioUtc = filtro.FechaInicio is { } inicio
            ? MedianocheLimaUtc(inicio)
            : null;
        DateTime? finExclusivoUtc = filtro.FechaFin is { } fin && fin < DateOnly.MaxValue
            ? MedianocheLimaUtc(fin.AddDays(1))
            : null;

        await using var transaccion =
            await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var eventos = await context.VwVentasPorEvento.AsNoTracking()
            .Where(v => v.OrganizadorId == organizadorId && v.EventoId.HasValue)
            .Where(v => !eventoId.HasValue || v.EventoId == eventoId)
            .OrderBy(v => v.NombreEvento)
            .ThenBy(v => v.EventoId)
            .Select(v => new DatosVentaEventoRaw
            {
                EventoId = v.EventoId!.Value,
                NombreEvento = v.NombreEvento ?? string.Empty,
                EstadoEvento = v.Estado ?? string.Empty,
                EntradasDisponibles = v.EntradasDisponibles ?? 0
            })
            .ToListAsync();

        if (eventos.Count == 0)
        {
            await transaccion.CommitAsync();
            return new DatosReporteRaw(eventos, []);
        }

        var filtroEvento = new NpgsqlParameter("reporte_evento", NpgsqlDbType.Integer)
        {
            Value = (object?)eventoId ?? DBNull.Value
        };
        var filtroInicio = new NpgsqlParameter("reporte_inicio", NpgsqlDbType.TimestampTz)
        {
            Value = (object?)inicioUtc ?? DBNull.Value
        };
        var filtroFin = new NpgsqlParameter("reporte_fin", NpgsqlDbType.TimestampTz)
        {
            Value = (object?)finExclusivoUtc ?? DBNull.Value
        };

        var ventas = await context.Database.SqlQuery<DatosVentaDiariaRaw>($"""
            SELECT t.evento_id AS "EventoId",
                   (t.fecha_emision AT TIME ZONE 'America/Lima')::date AS "Fecha",
                   btrim(c.moneda) AS "Moneda",
                   (count(*) FILTER (WHERE t.estado = 'Valida'))::integer AS "EntradasValidas",
                   (count(*) FILTER (WHERE t.estado = 'Utilizada'))::integer AS "EntradasUtilizadas",
                   (count(*) FILTER (WHERE t.estado = 'Anulada'))::integer AS "EntradasAnuladas",
                   (count(*) FILTER (
                       WHERE t.estado = 'Anulada'
                         AND t.estado_reembolso = 'PendienteReembolso'
                         AND t.precio_pagado > 0
                   ))::integer AS "EntradasPendientesReembolso",
                   coalesce(sum(t.precio_pagado) FILTER (
                       WHERE t.estado IN ('Valida', 'Utilizada')
                   ), 0) AS "Ingresos",
                   coalesce(sum(t.precio_pagado) FILTER (
                       WHERE t.estado = 'Anulada'
                         AND t.estado_reembolso = 'PendienteReembolso'
                         AND t.precio_pagado > 0
                   ), 0) AS "MontoPendienteReembolso"
            FROM public.entradas t
            JOIN public.eventos e ON e.evento_id = t.evento_id
            JOIN public.compras c ON c.compra_id = t.compra_id
            CROSS JOIN (
                SELECT {filtroEvento} AS evento_id, {filtroInicio} AS inicio_utc,
                       {filtroFin} AS fin_utc
            ) filtro
            WHERE e.organizador_id = {organizadorId}
              AND (filtro.evento_id IS NULL OR t.evento_id = filtro.evento_id)
              AND (filtro.inicio_utc IS NULL OR t.fecha_emision >= filtro.inicio_utc)
              AND (filtro.fin_utc IS NULL OR t.fecha_emision < filtro.fin_utc)
            GROUP BY 1, 2, 3
            """).ToListAsync();

        await transaccion.CommitAsync();
        return new DatosReporteRaw(eventos, ventas);
    }

    private static DateTime MedianocheLimaUtc(DateOnly fecha) =>
        TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(TimeOnly.MinValue), ZonaLima);
}