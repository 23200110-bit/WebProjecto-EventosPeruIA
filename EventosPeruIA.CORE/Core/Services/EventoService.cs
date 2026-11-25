using System.ComponentModel.DataAnnotations;
using EventosPeruIA.CORE.Core.DTOs.Eventos;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Services
{
    public class EventoService : IEventoService
    {
        private readonly IEventoRepository _eventoRepository;
        private readonly IAvisoEventoService _avisos;

        public EventoService(IEventoRepository eventoRepository, IAvisoEventoService avisos)
        {
            _eventoRepository = eventoRepository;
            _avisos = avisos;
        }

        // Hora de Lima (UTC-5, sin horario de verano) para los textos de los avisos.
        private static string HoraLima(DateTime utc) => utc.AddHours(-5).ToString("dd/MM/yyyy HH:mm");

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
                ImagenUrl = e.ImagenUrl,
                NombreLugar = e.NombreLugar,
                TicketsVendidos = entradasVendidas,
                CuposDisponibles = cuposDisponibles,
                Agotado = cuposDisponibles == 0
            };
        }

        public async Task<int> CreateEventoAsync(int userId, EventoCreateDto dto)
        {
            if (dto.Aforo <= 0) throw new ValidationException("El aforo debe ser un número entero mayor a cero.");
            if (dto.Precio < 0) throw new ValidationException("El precio no puede ser negativo.");

            var fechaInicio = dto.FechaInicio.UtcDateTime;
            var fechaFin = dto.FechaFin?.UtcDateTime;

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

            if (dto.VersionFila.HasValue && dto.VersionFila.Value != existing.VersionFila)
                throw new ProyectoException(
                    409,
                    "El evento cambió mientras lo editabas. Recarga la página y vuelve a intentarlo.");

            if (existing.Estado != "Borrador" && existing.Estado != "Publicado") throw new ValidationException("Solo se pueden editar eventos en estado Borrador o Publicado.");

            if (dto.Aforo <= 0) throw new ValidationException("El aforo debe ser mayor a cero.");
            if (dto.Precio < 0) throw new ValidationException("El precio no puede ser negativo.");

            int entradasActivas = await _eventoRepository.CountEntradasActivasAsync(dto.EventoId);
            if (dto.Aforo < entradasActivas) throw new ValidationException($"El aforo ({dto.Aforo}) no puede ser menor a las entradas ya vendidas ({entradasActivas}).");

            var fechaInicio = dto.FechaInicio.UtcDateTime;
            var fechaFin = dto.FechaFin?.UtcDateTime;

            if (fechaInicio == default)
                throw new ValidationException("La fecha de inicio es obligatoria.");

            if (fechaFin.HasValue && fechaFin.Value <= fechaInicio)
                throw new ValidationException("La fecha de fin debe ser posterior a la fecha de inicio.");

            var vaAPublicar = existing.Estado == "Borrador" && dto.Publicar;

            if ((vaAPublicar || existing.FechaInicio != fechaInicio) && fechaInicio <= DateTime.UtcNow)
                throw new ValidationException("La fecha de inicio debe ser posterior a la fecha actual.");

            if (existing.CategoriaId != dto.CategoriaId &&
                !await _eventoRepository.CategoriaExistsAsync(dto.CategoriaId))
                throw new ValidationException("La categoría especificada no existe o no está activa.");

            bool cambioProgramacion = existing.FechaInicio != fechaInicio || existing.FechaFin != fechaFin || existing.Direccion != dto.Direccion;
            bool cambioPrecio = existing.Precio != dto.Precio;
            string nombreAnterior = existing.Nombre;
            decimal precioAnterior = existing.Precio;
            DateTime inicioAnterior = existing.FechaInicio;
            string direccionAnterior = existing.Direccion;
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
            existing.ImagenUrl = dto.ImagenUrl;
            existing.NombreLugar = dto.NombreLugar;
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

                bool avisarEstado = existing.Estado == "Publicado" && (cambioProgramacion || cambioPrecio);
                if (avisarEstado)
                {
                    var cambios = new List<string>();
                    if (existing.FechaInicio != inicioAnterior)
                        cambios.Add($"Fecha y hora: antes {HoraLima(inicioAnterior)}, ahora {HoraLima(existing.FechaInicio)}.");
                    if (existing.Direccion != direccionAnterior)
                        cambios.Add($"Lugar: antes {direccionAnterior}, ahora {existing.Direccion}.");
                    if (cambioPrecio)
                        cambios.Add($"Precio de entrada: antes S/ {precioAnterior:0.00}, ahora S/ {existing.Precio:0.00} (tus entradas ya compradas no cambian).");
                    var titulo = cambioProgramacion ? $"Cambios en {existing.Nombre}" : $"Actualización de {existing.Nombre}";
                    var mensaje = $"El organizador actualizó \"{existing.Nombre}\". " + string.Join(" ", cambios);
                    var compradores = await _avisos.CompradoresValidosAsync(existing.EventoId);
                    await _avisos.AvisarAsync(existing.EventoId, compradores, titulo, mensaje);
                }
            }

            return actualizado;
        }

        public async Task<bool> CancelEventoAsync(int id, int userId, EventoCancelDto dto)
        {
            var descripcion = dto.DescripcionAdicional?.Trim();

            if (string.IsNullOrEmpty(descripcion))
                throw new ValidationException("Es obligatorio ingresar una descripción justificada de la cancelación.");

            if (descripcion.Length > 500)
                throw new ValidationException("La descripción no puede superar los 500 caracteres.");

            // Se leen antes de cancelar: la cancelación anula las entradas y ya no aparecerían como válidas.
            var evento = await _eventoRepository.GetEventoByIdAsync(id);
            var compradores = await _avisos.CompradoresValidosAsync(id);
            var inicioCancelacion = DateTime.UtcNow.AddSeconds(-5);

            await _eventoRepository.CancelarEventoEnBaseAsync(
                id,
                userId,
                dto.MotivoCancelacionId,
                descripcion);

            var nombre = evento?.Nombre ?? "el evento";
            await _avisos.AvisarAsync(
                id,
                compradores,
                $"Evento cancelado: {nombre}",
                $"\"{nombre}\" fue cancelado por el organizador. Motivo: {descripcion} Tus entradas quedan anuladas y se marcarán para reembolso.",
                omitirPantallaSiAvisadosDesde: inicioCancelacion);

            return true;
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

        public async Task<List<EventoDetalleDto>> ListarMisEventosAsync(int userId)
        {
            var eventos = await _eventoRepository.ListarEventosPorOrganizadorAsync(userId);
            var resultado = new List<EventoDetalleDto>();

            foreach (var e in eventos)
            {
                int entradasVendidas = await _eventoRepository.CountEntradasActivasAsync(e.EventoId);
                int cuposDisponibles = Math.Max(0, e.Aforo - entradasVendidas);

                resultado.Add(new EventoDetalleDto
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
                    EstadoModeracion = e.EstadoModeracion ?? "Aprobado",
                    ImagenUrl = e.ImagenUrl,
                    NombreLugar = e.NombreLugar,
                    TicketsVendidos = entradasVendidas,
                    CuposDisponibles = cuposDisponibles,
                    Agotado = cuposDisponibles == 0
                });
            }

            return resultado;
        }

        public async Task<EventoDetalleDto?> ObtenerMiEventoDetalleAsync(int id, int userId)
        {
            var e = await _eventoRepository.GetEventoByIdAsync(id);
            if (e == null || e.OrganizadorId != userId)
                return null;

            int entradasVendidas = await _eventoRepository.CountEntradasActivasAsync(id);
            int cuposDisponibles = Math.Max(0, e.Aforo - entradasVendidas);

            return new EventoDetalleDto
            {
                EventoId = e.EventoId,
                VersionFila = e.VersionFila,
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
                EstadoModeracion = e.EstadoModeracion ?? "Aprobado",
                ImagenUrl = e.ImagenUrl,
                NombreLugar = e.NombreLugar,
                TicketsVendidos = entradasVendidas,
                CuposDisponibles = cuposDisponibles,
                Agotado = cuposDisponibles == 0
            };
        }
    }
}