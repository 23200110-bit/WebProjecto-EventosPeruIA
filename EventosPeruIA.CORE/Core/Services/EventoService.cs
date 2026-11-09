using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Eventos;
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

        public async Task<List<EventoDto>> ListarEventosAsync(EventoFiltroQueryDto filtro)
        {
            var eventos = await _eventoRepository.ListarEventosPublicadosAsync(filtro.CategoriaId, filtro.Ubicacion, filtro.Fecha);
            return eventos.Select(e => new EventoDto
            {
                EventoId = e.EventoId,
                OrganizadorId = e.OrganizadorId,
                Categoria = new CategoriaResumenDto
                {
                    CategoriaId = e.CategoriaId,
                    Nombre = e.Categoria?.Nombre ?? string.Empty
                },
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                FechaInicio = e.FechaInicio,
                FechaFin = e.FechaFin,
                Direccion = e.Direccion,
                Ciudad = e.Ciudad,
                Latitud = e.Latitud,
                Longitud = e.Longitud,
                Aforo = e.Aforo,
                Precio = e.Precio,
                Estado = e.Estado,
                ImagenUrl = e.ImagenUrl,
                NombreLugar = e.NombreLugar
            }).ToList();
        }

        public async Task<EventoDetalleDto?> ObtenerDetalleEventoAsync(int id)
        {
            var e = await _eventoRepository.GetEventoByIdAsync(id);
            if (e == null || e.Estado != "Publicado" || e.EstadoModeracion != "Aprobado" || e.Organizador.Estado != "Activo")
                return null;

            int entradasVendidas = await _eventoRepository.CountEntradasActivasAsync(id);
            int cuposDisponibles = Math.Max(0, e.Aforo - entradasVendidas);

            return new EventoDetalleDto
            {
                EventoId = e.EventoId,
                OrganizadorId = e.OrganizadorId,
                Categoria = new CategoriaResumenDto
                {
                    CategoriaId = e.CategoriaId,
                    Nombre = e.Categoria?.Nombre ?? string.Empty
                },
                Nombre = e.Nombre,
                Descripcion = e.Descripcion,
                FechaInicio = e.FechaInicio,
                FechaFin = e.FechaFin,
                Direccion = e.Direccion,
                Ciudad = e.Ciudad,
                Latitud = e.Latitud,
                Longitud = e.Longitud,
                Aforo = e.Aforo,
                Precio = e.Precio,
                Estado = e.Estado,
                TicketsVendidos = entradasVendidas,
                CuposDisponibles = cuposDisponibles,
                Agotado = cuposDisponibles == 0
            };
        }

        public async Task<int> CreateEventoAsync(int userId, EventoCreateDto dto)
        {
            if (dto.Aforo <= 0) throw new ValidationException("El aforo debe ser un número entero mayor a cero.");
            if (dto.Precio < 0) throw new ValidationException("El precio no puede ser negativo.");

            var fechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc);
            DateTime? fechaFin = dto.FechaFin.HasValue ? DateTime.SpecifyKind(dto.FechaFin.Value, DateTimeKind.Utc) : null;

            if (fechaInicio <= DateTime.UtcNow) throw new ValidationException("La fecha de inicio debe ser posterior a la fecha actual.");
            if (fechaFin.HasValue && fechaFin.Value <= fechaInicio) throw new ValidationException("La fecha de fin debe ser posterior a la fecha de inicio.");
            if (!await _eventoRepository.CategoriaExistsAsync(dto.CategoriaId)) throw new ValidationException("La categoría especificada no existe o no está activa.");

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
                FechaPublicacion = dto.Publicar ? DateTime.UtcNow : null,
                ImagenUrl = dto.ImagenUrl,
                NombreLugar = dto.NombreLugar
            };

            return await _eventoRepository.CreateEventoAsync(evento);
        }

        public async Task<bool> UpdateEventoAsync(int userId, EventoUpdateDto dto)
        {
            var existing = await _eventoRepository.GetEventoByIdAsync(dto.EventoId);
            if (existing == null) throw new KeyNotFoundException("El evento especificado no existe.");
            if (existing.OrganizadorId != userId) throw new UnauthorizedAccessException("Acceso denegado: No es el propietario del evento.");
            if (existing.Estado != "Borrador" && existing.Estado != "Publicado") throw new ValidationException("Solo se pueden editar eventos en estado Borrador o Publicado.");

            if (dto.Aforo <= 0) throw new ValidationException("El aforo debe ser mayor a cero.");
            if (dto.Precio < 0) throw new ValidationException("El precio no puede ser negativo.");

            int entradasActivas = await _eventoRepository.CountEntradasActivasAsync(dto.EventoId);
            if (dto.Aforo < entradasActivas) throw new ValidationException($"El aforo ({dto.Aforo}) no puede ser menor a las entradas ya vendidas ({entradasActivas}).");

            var fechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc);
            DateTime? fechaFin = dto.FechaFin.HasValue ? DateTime.SpecifyKind(dto.FechaFin.Value, DateTimeKind.Utc) : null;

            bool cambioProgramacion = existing.FechaInicio != fechaInicio || existing.FechaFin != fechaFin || existing.Direccion != dto.Direccion;
            bool cambioContenido = cambioProgramacion || existing.Nombre != dto.Nombre || existing.Descripcion != dto.Descripcion || existing.Precio != dto.Precio;

            existing.CategoriaId = dto.CategoriaId;
            existing.Nombre = dto.Nombre.Trim();
            existing.Descripcion = dto.Descripcion.Trim();
            existing.FechaInicio = fechaInicio;
            existing.FechaFin = fechaFin;
            existing.Direccion = dto.Direccion.Trim();
            existing.Ciudad = dto.Ciudad.Trim();
            existing.Latitud = dto.Latitud;
            existing.Longitud = dto.Longitud;
            existing.Aforo = dto.Aforo;
            existing.Precio = dto.Precio;
            existing.FechaActualizacion = DateTime.UtcNow;

            if (dto.Publicar && existing.Estado == "Borrador")
            {
                existing.Estado = "Publicado";
                existing.FechaPublicacion = DateTime.UtcNow;
            }

            bool actualizado = await _eventoRepository.UpdateEventoAsync(existing);

            if (actualizado)
            {
                if (cambioContenido)
                {
                    await _eventoRepository.InactivarEmbeddingsVigentesAsync(existing.EventoId);
                }

                if (cambioProgramacion)
                {
                    await _eventoRepository.CrearNotificacionesCompradoresAsync(
                        existing.EventoId,
                        "CambioEvento",
                        $"El evento \"{existing.Nombre}\" ha cambiado su horario o ubicación. Revisa los nuevos detalles."
                    );
                }
            }

            return actualizado;
        }

        public async Task<bool> CancelEventoAsync(int id, int userId, EventoCancelDto dto)
        {
            var existing = await _eventoRepository.GetEventoByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("El evento especificado no existe.");
            if (existing.OrganizadorId != userId) throw new UnauthorizedAccessException("Acceso denegado: No es el propietario del evento.");
            if (existing.Estado != "Borrador" && existing.Estado != "Publicado") throw new ValidationException("Solo se pueden cancelar eventos en estado Borrador o Publicado.");
            if (existing.FechaInicio <= DateTime.UtcNow) throw new ValidationException("No se puede cancelar un evento que ya inició.");
            if (!await _eventoRepository.MotivoCancelacionExistsAsync(dto.MotivoCancelacionId)) throw new ValidationException("El motivo de cancelación especificado no existe.");

            int entradasVendidas = await _eventoRepository.CountEntradasActivasAsync(id);
            if (entradasVendidas > 0 && string.IsNullOrWhiteSpace(dto.DescripcionAdicional))
            {
                throw new ValidationException("Para eventos con entradas vendidas es obligatorio ingresar una descripción justificada de la cancelación.");
            }

            existing.Estado = "Cancelado";
            existing.FechaActualizacion = DateTime.UtcNow;

            bool cancelado = await _eventoRepository.UpdateEventoAsync(existing);

            if (cancelado)
            {
                await _eventoRepository.InactivarEmbeddingsVigentesAsync(id);
                await _eventoRepository.CancelarEntradasYMarcarReembolsoAsync(id);
                await _eventoRepository.CrearNotificacionesCompradoresAsync(
                    id,
                    "CancelacionEvento",
                    $"El evento \"{existing.Nombre}\" ha sido cancelado. Motivo registrado: {dto.DescripcionAdicional}"
                );
            }

            return cancelado;
        }

        public async Task<bool> PatchEventoAsync(int id, int userId, EventoPatchDto dto)
        {
            var existing = await _eventoRepository.GetEventoByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("El evento no existe.");
            if (existing.OrganizadorId != userId) throw new UnauthorizedAccessException("No tiene permisos para modificar este evento.");

            bool cambios = false;

            if (dto.ImagenUrl != null) { existing.ImagenUrl = dto.ImagenUrl; cambios = true; }
            if (dto.NombreLugar != null) { existing.NombreLugar = dto.NombreLugar; cambios = true; }
            if (dto.Nombre != null) { existing.Nombre = dto.Nombre.Trim(); cambios = true; }
            if (dto.Descripcion != null) { existing.Descripcion = dto.Descripcion.Trim(); cambios = true; }
            if (dto.Ciudad != null) { existing.Ciudad = dto.Ciudad.Trim(); cambios = true; }
            if (dto.Latitud.HasValue) { existing.Latitud = dto.Latitud; cambios = true; }
            if (dto.Longitud.HasValue) { existing.Longitud = dto.Longitud; cambios = true; }
            if (dto.Precio.HasValue) { existing.Precio = dto.Precio.Value; cambios = true; }
            if (dto.MaxEntradasPorCompra.HasValue) { existing.MaxEntradasPorCompra = dto.MaxEntradasPorCompra; cambios = true; }
            if (dto.EsGratuito.HasValue) { existing.EsGratuito = dto.EsGratuito; cambios = true; }

            if (!cambios) return true;

            existing.FechaActualizacion = DateTime.UtcNow;
            return await _eventoRepository.UpdateEventoAsync(existing);
        }
    }
}