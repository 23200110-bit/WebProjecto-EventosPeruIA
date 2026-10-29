using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.CORE.Core.Services
{
    public class EventoService : IEventoService
    {
        private readonly IEventoRepository _eventoRepository;

        public EventoService(IEventoRepository eventoRepository)
        {
            _eventoRepository = eventoRepository;
        }

        public async Task<EventoDTO?> GetEventoById(int id)
        {
            var evento = await _eventoRepository.GetEventoById(id);
            if (evento == null) return null;

            return new EventoDTO
            {
                EventoId = evento.EventoId,
                OrganizadorId = evento.OrganizadorId,
                CategoriaId = evento.CategoriaId,
                Nombre = evento.Nombre,
                Descripcion = evento.Descripcion,
                FechaInicio = evento.FechaInicio,
                FechaFin = evento.FechaFin,
                Direccion = evento.Direccion,
                Latitud = evento.Latitud,
                Longitud = evento.Longitud,
                Aforo = evento.Aforo,
                Precio = evento.Precio,
                Estado = evento.Estado
            };
        }

        public async Task<int> CreateEvento(int userId, EventoCreateDTO dto)
        {
            var fechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc);
            DateTime? fechaFin = dto.FechaFin.HasValue
                ? DateTime.SpecifyKind(dto.FechaFin.Value, DateTimeKind.Utc)
                : null;

            if (fechaInicio <= DateTime.UtcNow) return 0;
            if (fechaFin.HasValue && fechaFin.Value <= fechaInicio) return 0;
            if (!await _eventoRepository.CategoriaExists(dto.CategoriaId)) return 0;

            var evento = new Eventos
            {
                OrganizadorId = userId,
                CategoriaId = dto.CategoriaId,
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Direccion = dto.Direccion.Trim(),
                Ciudad = dto.Ciudad.Trim(),
                Latitud = dto.Latitud,
                Longitud = dto.Longitud,
                Aforo = dto.Aforo,
                Precio = dto.Precio,
                Estado = dto.Publicar ? "Publicado" : "Borrador",
                FechaCreacion = DateTime.UtcNow,
                FechaPublicacion = dto.Publicar ? DateTime.UtcNow : null
            };

            return await _eventoRepository.CreateEvento(evento);
        }

        public async Task<bool> UpdateEvento(EventoUpdateDTO dto)
        {
            var existing = await _eventoRepository.GetEventoById(dto.EventoId);
            if (existing == null) return false;
            if (existing.Estado != "Borrador" && existing.Estado != "Publicado") return false;

            var fechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc);
            DateTime? fechaFin = dto.FechaFin.HasValue
                ? DateTime.SpecifyKind(dto.FechaFin.Value, DateTimeKind.Utc)
                : null;

            if (fechaInicio <= DateTime.UtcNow) return false;
            if (fechaFin.HasValue && fechaFin.Value <= fechaInicio) return false;
            if (!await _eventoRepository.CategoriaExists(dto.CategoriaId)) return false;

            var entradasActivas = await _eventoRepository.CountEntradasActivas(dto.EventoId);
            if (dto.Aforo < entradasActivas) return false;

            var publicar = dto.Publicar && existing.Estado == "Borrador";

            var evento = new Eventos
            {
                EventoId = dto.EventoId,
                CategoriaId = dto.CategoriaId,
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Direccion = dto.Direccion.Trim(),
                Ciudad = dto.Ciudad.Trim(),
                Latitud = dto.Latitud,
                Longitud = dto.Longitud,
                Aforo = dto.Aforo,
                Precio = dto.Precio,
                Estado = publicar ? "Publicado" : existing.Estado,
                FechaPublicacion = publicar ? DateTime.UtcNow : existing.FechaPublicacion
            };

            return await _eventoRepository.UpdateEvento(evento);
        }

        public async Task<bool> CancelEvento(int id, int userId, EventoCancelDTO dto)
        {
            var existing = await _eventoRepository.GetEventoById(id);
            if (existing == null) return false;
            if (existing.Estado != "Borrador" && existing.Estado != "Publicado") return false;
            if (existing.FechaInicio <= DateTime.UtcNow) return false;
            if (!await _eventoRepository.MotivoCancelacionExists(dto.MotivoCancelacionId)) return false;

            return await _eventoRepository.CancelEvento(
                id,
                userId,
                dto.MotivoCancelacionId,
                dto.DescripcionAdicional.Trim());
        }

        public async Task<bool> PatchEvento(int id, int userId, EventoPatchDTO dto)
        {
            var existing = await _eventoRepository.GetEventoById(id);
            if (existing == null) return false;
            if (existing.OrganizadorId != userId) return false;

            var cambios = false;

            if (dto.ImagenUrl != null)
            {
                existing.ImagenUrl = dto.ImagenUrl;
                cambios = true;
            }

            if (dto.NombreLugar != null)
            {
                existing.NombreLugar = dto.NombreLugar;
                cambios = true;
            }

            if (dto.Nombre != null)
            {
                existing.Nombre = dto.Nombre.Trim();
                cambios = true;
            }

            if (dto.Descripcion != null)
            {
                existing.Descripcion = dto.Descripcion.Trim();
                cambios = true;
            }

            if (dto.Ciudad != null)
            {
                existing.Ciudad = dto.Ciudad.Trim();
                cambios = true;
            }

            if (dto.Latitud.HasValue)
            {
                existing.Latitud = dto.Latitud;
                cambios = true;
            }

            if (dto.Longitud.HasValue)
            {
                existing.Longitud = dto.Longitud;
                cambios = true;
            }

            if (dto.Precio.HasValue)
            {
                existing.Precio = dto.Precio.Value;
                cambios = true;
            }

            if (dto.MaxEntradasPorCompra.HasValue)
            {
                existing.MaxEntradasPorCompra = dto.MaxEntradasPorCompra;
                cambios = true;
            }

            if (dto.EsGratuito.HasValue)
            {
                existing.EsGratuito = dto.EsGratuito;
                cambios = true;
            }

            if (!cambios) return true;

            existing.FechaActualizacion = DateTime.UtcNow;

            return await _eventoRepository.UpdateEvento(existing);
        }

    }
}