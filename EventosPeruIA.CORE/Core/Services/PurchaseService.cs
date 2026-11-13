using System.Data;
using System.Security.Cryptography;
using System.Text;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace EventosPeruIA.CORE.Core.Services;
public sealed class PurchaseService(PostgresContext db,TimeProvider clock,IConfiguration config) {
 public async Task<PurchaseResponse> Buy(int buyerId,PurchaseRequest request,CancellationToken ct) {
  if(request.RequestId==Guid.Empty||request.EventoId<=0||request.Cantidad<1||request.Cantidad>100)
   throw new ProyectoException(400,"Evento, cantidad y RequestId UUID son obligatorios.");
  var key="WEB-"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{buyerId}:{request.RequestId:D}")))[..40];
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
  // Serialize retries independently of event IDs, before taking the event lock.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",ct);
  var buyer=await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u=>u.UsuarioId==buyerId,ct);
  if(buyer?.Estado!="Activo")throw new ProyectoException(403,"La cuenta no está activa.");
  var previous=await db.Compras.Include(c=>c.Entradas).SingleOrDefaultAsync(c=>c.CodigoTransaccion==key,ct);
  if(previous!=null) {
   if(previous.CompradorId!=buyerId||previous.EventoId!=request.EventoId||previous.CantidadEntradas!=request.Cantidad)
    throw new ProyectoException(409,"RequestId ya utilizado para otra compra.");
   await tx.CommitAsync(ct);return Map(previous);
  }
  var e=await db.Eventos.FromSqlInterpolated($"SELECT * FROM eventos WHERE evento_id={request.EventoId} FOR UPDATE").SingleOrDefaultAsync(ct);
  var now=clock.GetUtcNow().UtcDateTime;
  if(e==null)throw new ProyectoException(404,"Evento no encontrado.");
  if(e.Estado!="Publicado"||e.EstadoModeracion!="Aprobado"||e.FechaInicio<=now)
   throw new ProyectoException(409,"El evento no está disponible para venta.");
  var limit=e.MaxEntradasPorCompra??config.GetValue<short>("Purchases:MaxTicketsPerOrder",10);
  if(limit<1||request.Cantidad>limit)throw new ProyectoException(400,$"El máximo por compra es {limit}.");
  var sold=await db.Entradas.CountAsync(t=>t.EventoId==e.EventoId&&(t.Estado=="Valida"||t.Estado=="Utilizada"),ct);
  if(e.Aforo-sold<request.Cantidad)throw new ProyectoException(409,"No quedan suficientes entradas disponibles.");
  var price=e.EsGratuito==true?0m:e.Precio;
  if(price<0||price*request.Cantidad>99999999.99m)throw new ProyectoException(409,"Precio o total de compra inválido.");
  var purchase=new Compras{CompradorId=buyerId,EventoId=e.EventoId,CantidadEntradas=request.Cantidad,
   PrecioUnitario=price,MontoTotal=price*request.Cantidad,Moneda=e.Moneda,MetodoPago="Simulado",
   CodigoTransaccion=key,Estado="Confirmada",FechaCompra=now};
  for(var n=0;n<request.Cantidad;n++) {
   var id=Guid.NewGuid();purchase.Entradas.Add(new Entradas{EntradaId=id,EventoId=e.EventoId,CompradorId=buyerId,
    TitularNombre=$"{buyer.Nombres} {buyer.Apellidos}",PrecioPagado=price,Estado="Valida",FechaEmision=now,
    CodigoCorto="TCK-"+id.ToString("N")[..10].ToUpperInvariant()});
  }
  db.Compras.Add(purchase);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Map(purchase);
 }
 static PurchaseResponse Map(Compras c)=>new(c.CompraId,c.EventoId,c.CantidadEntradas,c.PrecioUnitario,c.MontoTotal,c.Moneda,c.MetodoPago,
  c.Entradas.OrderBy(t=>t.EntradaId).Select(t=>new PurchasedTicket(t.EntradaId,t.CodigoCorto??"",t.EntradaId.ToString())).ToArray());
}
