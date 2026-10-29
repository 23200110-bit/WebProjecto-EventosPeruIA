using EventosPeruIA.CORE.Core.DTOs.Personal;
using EventosPeruIA.CORE.Core.Interfaces.Repositories;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace EventosPeruIA.CORE.Infrastructure.Repositories;
public class PersonalRepository : IPersonalRepository
{
 private readonly PostgresContext _context;
 public PersonalRepository(PostgresContext context){_context=context;}
 public async Task<List<TicketResponse>> GetTickets(int userId)
 {
  return await _context.Entradas.AsNoTracking().Where(t=>t.CompradorId==userId).OrderByDescending(t=>t.FechaEmision).Take(100)
   .Select(t=>new TicketResponse {TicketId=t.EntradaId,EventId=t.EventoId,Event=t.Evento.Nombre,
    Holder=t.TitularNombre ?? (t.Comprador.Nombres+" "+t.Comprador.Apellidos),ShortCode=t.CodigoCorto,Status=t.Estado,
    EventStatus=t.Evento.Estado,RefundStatus=t.EstadoReembolso,
    CancellationReason=t.Evento.EventosCancelacion==null?null:t.Evento.EventosCancelacion.DescripcionAdicional,
    Venue=t.Evento.NombreLugar,Address=t.Evento.Direccion,City=t.Evento.Ciudad,StartDate=t.Evento.FechaInicio,
    Price=t.PrecioPagado,Currency=t.Evento.Moneda}).ToListAsync();
 }
 public async Task<List<NotificationResponse>> GetNotifications(int userId)
 {
  return await _context.Notificaciones.AsNoTracking().Where(n=>n.UsuarioId==userId).OrderByDescending(n=>n.FechaCreacion).Take(100)
   .Select(n=>new NotificationResponse {Id=n.NotificacionId,Title=n.Titulo,Message=n.Mensaje,Read=n.Leida,CreatedAt=n.FechaCreacion}).ToListAsync();
 }
 public async Task<bool> MarkRead(int userId,long id)
 {
  var notification=await _context.Notificaciones.FirstOrDefaultAsync(n=>n.UsuarioId==userId&&n.NotificacionId==id);
  if(notification==null)return false;
  if(!notification.Leida){notification.Leida=true;notification.FechaLectura=DateTime.UtcNow;await _context.SaveChangesAsync();}
  return true;
 }
}
