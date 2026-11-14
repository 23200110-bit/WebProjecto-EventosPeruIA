using EventosPeruIA.CORE.Core.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.DTOs.Auth;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Services;
using EventosPeruIA.CORE.Core.Settings;
using EventosPeruIA.CORE.Infrastructure.Data;
using EventosPeruIA.CORE.Infrastructure.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization=true)]
public sealed class FeaturePostgresFactAttribute:FactAttribute {
 public FeaturePostgresFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EVENTOS_FEATURE_TEST_CONNECTION")))Skip="Requiere PostgreSQL desechable exclusivo EVENTOS_FEATURE_TEST_CONNECTION.";}
}
public class PurchasePasswordTests {
 static string Connection=>Environment.GetEnvironmentVariable("EVENTOS_FEATURE_TEST_CONNECTION")!;
 const string Secret="Synthetic-feature-test-key-012345678901234567890123456789";
 static PostgresContext Db()=>new(new DbContextOptionsBuilder<PostgresContext>().UseNpgsql(Connection).Options);
 sealed class Mail:IPasswordResetMail {
  public List<string> Links=[];public bool Fail;public bool Configured=true;
  public void ValidateConfiguration(){if(!Configured)throw new InvalidOperationException();}
  public Task Send(string recipient,string resetUrl,CancellationToken ct){if(Fail)throw new IOException("Synthetic delivery failure");Links.Add(resetUrl);return Task.CompletedTask;}
  public string Token=>Uri.UnescapeDataString(new Uri(Links.Last()).Fragment[7..]);
 }
 sealed class Factory(Mail mail,bool local=false,string? pickup=null):WebApplicationFactory<Program> {
  protected override void ConfigureWebHost(IWebHostBuilder b){
   b.UseEnvironment(local?"Development":"Testing");b.UseContentRoot("/work/EventosPeruIA.API");
   b.UseSetting("ConnectionStrings:DevConnection",Connection);b.UseSetting("JWTSettings:SecretKey",Secret);
   b.UseSetting("JWTSettings:Issuer","EventosPeruIA");b.UseSetting("JWTSettings:Audience","EventosPeruIA.Cliente");
   b.UseSetting("PasswordReset:PublicResetUrl",local?"http://localhost:5279/recuperar-contrasena":"https://eventos.example/reset-password");
   if(local){b.UseSetting("PasswordReset:DeliveryMode","Local");b.UseSetting("PasswordReset:PickupDirectory",pickup);}
   b.ConfigureServices(s=>{if(!local){s.RemoveAll<IPasswordResetMail>();s.AddSingleton<IPasswordResetMail>(mail);}});
  }
 }
 sealed class Scenario: IAsyncDisposable {
  public string? Pickup;public Mail Mail=new();public Factory Factory=null!;public HttpClient Client=null!;public int Buyer;public int Other;public int Event;
  public async ValueTask DisposeAsync(){Client.Dispose();await Factory.DisposeAsync();}
 }
 static async Task<Scenario> Setup(int capacity=10,decimal price=25m,bool local=false){
  var cs=new NpgsqlConnectionStringBuilder(Connection);
  if(cs.Database!="eventos_feature_test")throw new InvalidOperationException("Only disposable eventos_feature_test is permitted.");
  await using(var bootstrap=new NpgsqlConnection(Connection)) {
   await bootstrap.OpenAsync();
   await new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS citext; CREATE EXTENSION IF NOT EXISTS unaccent; CREATE OR REPLACE FUNCTION public.fn_unaccent(text) RETURNS text LANGUAGE sql IMMUTABLE PARALLEL SAFE AS 'SELECT public.unaccent(''public.unaccent'', $1)';",bootstrap).ExecuteNonQueryAsync();
   await bootstrap.ReloadTypesAsync();
  }
  await using var db=Db();
  if(!await db.Database.CanConnectAsync())throw new Exception("Disposable PostgreSQL unavailable");
  await using(var c=new NpgsqlConnection(Connection)){await c.OpenAsync();var exists=await new NpgsqlCommand("SELECT to_regclass('public.usuarios') IS NOT NULL",c).ExecuteScalarAsync();if(!(bool)exists!)await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());}
  await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE usuarios, categorias CASCADE");
  var buyer=new Usuarios{Nombres="Cliente",Apellidos="Prueba",Correo="cliente@example.test",Estado="Activo",ProveedorAuth="Local",ContrasenaHash="pending",FechaRegistro=DateTime.UtcNow};
  buyer.ContrasenaHash=new PasswordHasher<Usuarios>().HashPassword(buyer,"OriginalPassword2026!");
  var other=new Usuarios{Nombres="Otro",Apellidos="Prueba",Correo="otro@example.test",Estado="Activo",ProveedorAuth="Local",ContrasenaHash=buyer.ContrasenaHash,FechaRegistro=DateTime.UtcNow};
  var cat=new Categorias{Nombre="Prueba"};db.AddRange(buyer,other,cat);await db.SaveChangesAsync();
  var e=new Eventos{OrganizadorId=other.UsuarioId,CategoriaId=cat.CategoriaId,Nombre="Evento sintético",Descripcion="Prueba",Direccion="Dirección",Ciudad="Lima",FechaInicio=DateTime.UtcNow.AddDays(5),Aforo=capacity,Precio=price,Moneda="PEN",Estado="Publicado",EstadoModeracion="Aprobado",FechaCreacion=DateTime.UtcNow,MaxEntradasPorCompra=5};
  db.Add(e);await db.SaveChangesAsync();
  var s=new Scenario{Buyer=buyer.UsuarioId,Other=other.UsuarioId,Event=e.EventoId};if(local)s.Pickup=Path.Combine(Path.GetTempPath(),"eventos-mail-"+Guid.NewGuid().ToString("N"));s.Factory=new Factory(s.Mail,local,s.Pickup);s.Client=s.Factory.CreateClient();return s;
 }
 static async Task SignIn(HttpClient c,string email="cliente@example.test",string password="OriginalPassword2026!"){
  var r=await c.PostAsJsonAsync("/api/auth/login",new{email,password});Assert.Equal(HttpStatusCode.OK,r.StatusCode);
  var result=await r.Content.ReadFromJsonAsync<LoginResponse>();c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",result!.AccessToken);
 }
 static async Task<PurchaseResponse> Buy(Scenario s,int qty=1,Guid? id=null){
  var r=await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=qty,requestId=id??Guid.NewGuid()});
  Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());return (await r.Content.ReadFromJsonAsync<PurchaseResponse>())!;
 }
 static async Task Forgot(Scenario s,string email="cliente@example.test"){
  var r=await s.Client.PostAsJsonAsync("/api/auth/forgot-password",new{email});Assert.Equal(HttpStatusCode.Accepted,r.StatusCode);
  Assert.DoesNotContain("token",(await r.Content.ReadAsStringAsync()).ToLowerInvariant());
 }
 static Task<HttpResponseMessage> Reset(Scenario s,string token,string password="ChangedPassword2026!")=>s.Client.PostAsJsonAsync("/api/auth/reset-password",new{token,password,confirmPassword=password});
 [FeaturePostgresFact]public async Task Compra_emite_UUID_voucher_precio_servidor_y_reintento_idempotente(){
  await using var s=await Setup();await SignIn(s.Client);var id=Guid.NewGuid();
  var a=await Buy(s,2,id);var b=await Buy(s,2,id);Assert.Equal(a.CompraId,b.CompraId);Assert.Equal(50m,a.MontoTotal);Assert.Equal("Simulado",a.MetodoPago);Assert.Equal(2,a.Entradas.Count);Assert.Equal(2,a.Entradas.Select(t=>t.EntradaId).Distinct().Count());
  await using var db=Db();Assert.Equal(1,await db.Compras.CountAsync());Assert.Equal(2,await db.Entradas.CountAsync());Assert.All(await db.Entradas.ToListAsync(),t=>Assert.Equal(s.Buyer,t.CompradorId));
  var voucher=await s.Client.GetAsync($"/api/Entradas/{a.Entradas[0].EntradaId}/voucher");Assert.Equal(HttpStatusCode.OK,voucher.StatusCode);
  var changed=await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=id});Assert.Equal(HttpStatusCode.Conflict,changed.StatusCode);
 }
 [FeaturePostgresFact]public async Task Compra_concurrente_ultima_plaza_solo_una_y_reintentos_no_duplican(){
  await using var s=await Setup(1);await SignIn(s.Client);using var other=s.Factory.CreateClient();await SignIn(other,"otro@example.test");
  var calls=new[]{s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()}),other.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()})};
  var rs=await Task.WhenAll(calls);Assert.Single(rs,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(rs,r=>r.StatusCode==HttpStatusCode.Conflict);
  await using var db=Db();Assert.Equal(1,await db.Compras.CountAsync());Assert.Equal(1,await db.Entradas.CountAsync());
 }
 [FeaturePostgresFact]public async Task Compra_reintentos_concurrentes_una_sola_venta(){
  await using var s=await Setup();await SignIn(s.Client);var id=Guid.NewGuid();
  var rs=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=2,requestId=id})));Assert.All(rs,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));
  await using var db=Db();Assert.Equal(1,await db.Compras.CountAsync());Assert.Equal(2,await db.Entradas.CountAsync());
 }
 [FeaturePostgresFact]public async Task Compra_rechaza_anonimo_cantidad_invalida_evento_no_vendible_y_falsificacion_precio(){
  await using var s=await Setup();var dto=new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()};Assert.Equal(HttpStatusCode.Unauthorized,(await s.Client.PostAsJsonAsync("/api/purchases",dto)).StatusCode);
  await SignIn(s.Client);Assert.Equal(HttpStatusCode.BadRequest,(await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=0,requestId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=6,requestId=Guid.NewGuid()})).StatusCode);
  var forged=await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid(),precioUnitario=0,compradorId=s.Other});Assert.Equal(HttpStatusCode.OK,forged.StatusCode);
  var p=(await forged.Content.ReadFromJsonAsync<PurchaseResponse>())!;Assert.Equal(25m,p.MontoTotal);
  await using var db=Db();Assert.Equal(s.Buyer,(await db.Compras.SingleAsync()).CompradorId);await db.Eventos.Where(e=>e.EventoId==s.Event).ExecuteUpdateAsync(u=>u.SetProperty(e=>e.EstadoModeracion,"Suspendido"));
  Assert.Equal(HttpStatusCode.Conflict,(await s.Client.PostAsJsonAsync("/api/purchases",dto)).StatusCode);
 }
 [FeaturePostgresFact]public async Task Compra_fallo_insertar_entrada_revierte_compra_completa(){
  await using var s=await Setup();await SignIn(s.Client);await using var db=Db();
  await db.Database.ExecuteSqlRawAsync("CREATE OR REPLACE FUNCTION fail_ticket_test() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'synthetic failure'; END $$; CREATE TRIGGER fail_ticket_test BEFORE INSERT ON entradas FOR EACH ROW EXECUTE FUNCTION fail_ticket_test();");
  try{
   try{var r=await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()});Assert.Equal(HttpStatusCode.InternalServerError,r.StatusCode);}catch(DbUpdateException){}
   Assert.Equal(0,await db.Compras.CountAsync());Assert.Equal(0,await db.Entradas.CountAsync());
  }finally{await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_ticket_test ON entradas; DROP FUNCTION fail_ticket_test();");}
 }
 [FeaturePostgresFact]public async Task Recuperacion_correo_hash_cambio_login_y_revocacion_de_sesion_previa(){
  await using var s=await Setup();await SignIn(s.Client);var old=s.Client.DefaultRequestHeaders.Authorization;
  await Forgot(s);var token=s.Mail.Token;await using var db=Db();var stored=await db.TokensRecuperacionContrasena.SingleAsync();Assert.NotEqual(token,stored.TokenHash);Assert.Equal(PasswordResetService.Hash(token),stored.TokenHash);
  Assert.Equal(HttpStatusCode.OK,(await Reset(s,token)).StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized,(await s.Client.GetAsync("/api/Entradas/mis-entradas")).StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized,(await s.Client.PostAsJsonAsync("/api/auth/login",new{email="cliente@example.test",password="OriginalPassword2026!"})).StatusCode);
  await SignIn(s.Client,password:"ChangedPassword2026!");Assert.Equal(HttpStatusCode.OK,(await s.Client.GetAsync("/api/Entradas/mis-entradas")).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Reset(s,token)).StatusCode);
 }
 [FeaturePostgresFact]public async Task Recuperacion_no_enumera_cuentas_y_no_devuelve_token(){
  await using var s=await Setup();var a=await s.Client.PostAsJsonAsync("/api/auth/forgot-password",new{email="cliente@example.test"});var b=await s.Client.PostAsJsonAsync("/api/auth/forgot-password",new{email="ausente@example.test"});
  Assert.Equal(a.StatusCode,b.StatusCode);Assert.Equal(await a.Content.ReadAsStringAsync(),await b.Content.ReadAsStringAsync());Assert.Single(s.Mail.Links);
  await Forgot(s);Assert.Single(s.Mail.Links);
 }
 [FeaturePostgresFact]public async Task Recuperacion_expiracion_token_invalido_y_confirmacion(){
  await using var s=await Setup();await Forgot(s);var token=s.Mail.Token;await using var db=Db();await db.TokensRecuperacionContrasena.ExecuteUpdateAsync(u=>u.SetProperty(t=>t.FechaExpiracion,DateTime.UtcNow.AddMinutes(-1)));
  Assert.Equal(HttpStatusCode.BadRequest,(await Reset(s,token)).StatusCode);Assert.Equal(HttpStatusCode.BadRequest,(await Reset(s,new string('x',43))).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await s.Client.PostAsJsonAsync("/api/auth/reset-password",new{token,password="LongPassword2026!",confirmPassword="OtherPassword!"})).StatusCode);
 }
 [FeaturePostgresFact]public async Task Recuperacion_concurrente_token_solo_se_consume_una_vez(){
  await using var s=await Setup();await Forgot(s);var rs=await Task.WhenAll(Reset(s,s.Mail.Token),Reset(s,s.Mail.Token));Assert.Single(rs,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(rs,r=>r.StatusCode==HttpStatusCode.BadRequest);
 }
 [FeaturePostgresFact]public async Task Recuperacion_fallo_correo_sin_token_persistido_y_configuracion_global(){
  await using var s=await Setup();s.Mail.Fail=true;await Forgot(s);await using var db=Db();Assert.Equal(0,await db.TokensRecuperacionContrasena.CountAsync());
  s.Mail.Configured=false;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await s.Client.PostAsJsonAsync("/api/auth/forgot-password",new{email="ausente@example.test"})).StatusCode);
 }
 [FeaturePostgresFact]public async Task Recuperacion_cuenta_bloqueada_o_solo_google_no_recibe_token(){
  await using var s=await Setup();await using var db=Db();await db.Usuarios.Where(u=>u.UsuarioId==s.Buyer).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.ProveedorAuth,"Google"));await Forgot(s);Assert.Empty(s.Mail.Links);
  await db.Usuarios.Where(u=>u.UsuarioId==s.Other).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.Estado,"Bloqueado"));await Forgot(s,"otro@example.test");Assert.Empty(s.Mail.Links);Assert.Equal(0,await db.TokensRecuperacionContrasena.CountAsync());
 }
 [FeaturePostgresFact]public async Task Compra_gratuita_y_rechazo_fecha_y_stock(){
  await using var s=await Setup(2,0m);await SignIn(s.Client);await using var db=Db();
  // es_gratuito es calculado por PostgreSQL a partir de precio=0; no se escribe.
  var p=await Buy(s,2);Assert.Equal(0m,p.MontoTotal);
  Assert.Equal(HttpStatusCode.Conflict,(await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()})).StatusCode);
  await db.Eventos.Where(e=>e.EventoId==s.Event).ExecuteUpdateAsync(u=>u.SetProperty(e=>e.FechaInicio,DateTime.UtcNow.AddDays(-1)));
  Assert.Equal(HttpStatusCode.Conflict,(await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await s.Client.PostAsJsonAsync("/api/purchases",new{eventoId=s.Event,cantidad=1,requestId=Guid.Empty})).StatusCode);
 }
 [FeaturePostgresFact]public async Task Recuperacion_invalida_todos_los_enlaces_previos(){
  await using var s=await Setup();await Forgot(s);var old=s.Mail.Token;await using var db=Db();
  await db.TokensRecuperacionContrasena.ExecuteUpdateAsync(u=>u.SetProperty(t=>t.FechaCreacion,DateTime.UtcNow.AddMinutes(-2)));
  await Forgot(s);Assert.Equal(2,s.Mail.Links.Count);var current=s.Mail.Token;
  Assert.Equal(HttpStatusCode.OK,(await Reset(s,current)).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Reset(s,old)).StatusCode);
  Assert.All(await db.TokensRecuperacionContrasena.AsNoTracking().ToListAsync(),t=>Assert.True(t.Utilizado));
 }

 [FeaturePostgresFact]public async Task Pagina_recuperacion_anonima_sin_cache_y_sin_recursos_externos(){
  await using var s=await Setup();var r=await s.Client.GetAsync("/recuperar-contrasena");
  Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Contains("no-store",r.Headers.CacheControl!.ToString());
  Assert.Contains("frame-ancestors 'none'",r.Headers.GetValues("Content-Security-Policy").Single());
  var html=await r.Content.ReadAsStringAsync();Assert.Contains("request-form",html);Assert.Contains("reset-form",html);
  Assert.DoesNotContain("<script src=",html);Assert.DoesNotContain("__NONCE__",html);
 }
 [FeaturePostgresFact]public async Task Buzon_local_real_entrega_enlace_y_recupera_sin_SMTP(){
  await using var s=await Setup(local:true);await Forgot(s);
  var files=Directory.GetFiles(s.Pickup!,"*.eml");Assert.Single(files);
  var message=await File.ReadAllTextAsync(files[0]);var body=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message[(message.IndexOf("\r\n\r\n")+4)..].Trim()));
  var url=body.Split('\n').Select(x=>x.Trim()).Single(x=>x.StartsWith("http://localhost:5279/recuperar-contrasena#token="));
  var token=Uri.UnescapeDataString(new Uri(url).Fragment[7..]);
  Assert.Equal(HttpStatusCode.OK,(await Reset(s,token)).StatusCode);await SignIn(s.Client,password:"ChangedPassword2026!");
  if(!OperatingSystem.IsWindows())Assert.Equal(UnixFileMode.UserRead|UnixFileMode.UserWrite,File.GetUnixFileMode(files[0]));
 }
 [Fact]public void Buzon_local_rechazado_fuera_de_Development(){
  var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"PasswordReset:PickupDirectory",Path.GetTempPath()}}).Build();
  var environment=new TestEnvironment{EnvironmentName="Production"};
  Assert.Throws<InvalidOperationException>(()=>new LocalPasswordResetMail(config,environment).ValidateConfiguration());
 }
 sealed class TestEnvironment:Microsoft.Extensions.Hosting.IHostEnvironment {
  public string EnvironmentName{get;set;}="Production";public string ApplicationName{get;set;}="Tests";public string ContentRootPath{get;set;}="/tmp";
  public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider{get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
 }

}
