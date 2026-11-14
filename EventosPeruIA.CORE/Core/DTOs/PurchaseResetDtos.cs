using System.ComponentModel.DataAnnotations;
namespace EventosPeruIA.CORE.Core.DTOs;
public sealed class PurchaseRequest {
 [Range(1,int.MaxValue)] public int EventoId {get;set;}
 [Range(1,100)] public int Cantidad {get;set;}
 public Guid RequestId {get;set;}
}
public sealed record PurchasedTicket(Guid EntradaId,string CodigoCorto,string QrPayload);
public sealed record PurchaseResponse(int CompraId,int EventoId,int Cantidad,decimal PrecioUnitario,decimal MontoTotal,string Moneda,string MetodoPago,IReadOnlyList<PurchasedTicket> Entradas);
public sealed class ForgotPasswordRequest {
 [Required,EmailAddress,StringLength(254)] public string Email {get;set;}="";
}
public sealed class ResetPasswordRequest {
 [Required,StringLength(128)] public string Token {get;set;}="";
 [Required,StringLength(128,MinimumLength=8)] public string Password {get;set;}="";
 [Required,Compare(nameof(Password))] public string ConfirmPassword {get;set;}="";
}
