using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Entities;
using EventosPeruIA.CORE.Core.Interfaces;
using EventosPeruIA.CORE.Infrastructure.Data;
using EventosPeruIA.CORE.Infrastructure.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

// Todos los registros de esta prueba se revierten al finalizar. No se crean tablas.
public sealed class SupabaseFactory(NpgsqlConnection connection, NpgsqlTransaction transaction, ModeloPrueba modelo, bool modeloReal = false) : WebApplicationFactory<Program>
{
    public List<string> SalidasIA { get; } = [];
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DevConnection", connection.ConnectionString);
        builder.UseSetting("JWTSettings:SecretKey", "Synthetic-supabase-test-key-012345678901234567890123456789");
        builder.UseSetting("JWTSettings:Issuer", "EventosPeruIA");
        builder.UseSetting("JWTSettings:Audience", "EventosPeruIA.Cliente");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PostgresContext>();
            services.RemoveAll<DbContextOptions<PostgresContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<PostgresContext>>();
            services.AddScoped(_ =>
            {
                var db = new PostgresContext(new DbContextOptionsBuilder<PostgresContext>().UseNpgsql(connection).Options);
                db.Database.UseTransaction(transaction);
                return db;
            });
            if (!modeloReal) { services.RemoveAll<IModeloIA>(); services.AddSingleton<IModeloIA>(modelo); }
            else
            {
                services.AddHttpClient<OllamaModeloIA>(http => { http.BaseAddress = new Uri("http://localhost:11434/"); http.Timeout = TimeSpan.FromMinutes(2); });
                services.RemoveAll<IModeloIA>();
                services.AddScoped<IModeloIA>(sp => new ModeloObservado(sp.GetRequiredService<OllamaModeloIA>(), SalidasIA));
            }
        });
    }
}

public sealed class SupabaseTests
{
    [SupabaseFact]
    public async Task Endpoints_reales_de_moderacion_asistencia_y_RAG_con_rollback()
    {

        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("EVENTOS_TEST_CONNECTION"));
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            var modelo = new ModeloPrueba { Filtros = "{\"ubicacion\":\"CIUDAD_PRUEBA_EDUARDO\"}" };
            using var factory = new SupabaseFactory(connection, tx, modelo, Environment.GetEnvironmentVariable("EVENTOS_TEST_OLLAMA") == "1");
            using var client = factory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PostgresContext>();
            var hash = new PasswordHasher<Usuarios>();
            var suffix = Guid.NewGuid().ToString("N");
            Usuarios Usuario(string nombre, bool admin = false)
            {
                var u = new Usuarios { Nombres = nombre, Apellidos = "PRUEBA_ROLLBACK", Correo = $"prueba-{nombre}-{suffix}@example.test", ProveedorAuth = "Local", EsAdministrador = admin, Estado = "Activo", FechaRegistro = DateTime.UtcNow };
                u.ContrasenaHash = hash.HashPassword(u, "PruebaTemporal2026!"); return u;
            }
            var admin = Usuario("admin", true); var org = Usuario("organizador"); var comprador = Usuario("comprador");
            db.Usuarios.AddRange(admin, org, comprador); await db.SaveChangesAsync();
            var categoria = await db.Categorias.Where(c => c.Activo).Select(c => c.CategoriaId).FirstAsync();
            var evento = new Eventos { Nombre = "PRUEBA_ROLLBACK_EDUARDO", Descripcion = "Evento sintético revertido al terminar", Direccion = "CIUDAD_PRUEBA_EDUARDO", Ciudad = "CIUDAD_PRUEBA_EDUARDO", CategoriaId = categoria, OrganizadorId = org.UsuarioId, FechaInicio = DateTime.UtcNow.AddDays(30), FechaFin = DateTime.UtcNow.AddDays(30).AddHours(3), Aforo = 10, Moneda = "PEN", Precio = 25, Estado = "Publicado", EstadoModeracion = "Aprobado", FechaCreacion = DateTime.UtcNow };
            db.Eventos.Add(evento); await db.SaveChangesAsync(); modelo.EventoId = evento.EventoId;
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT public.fn_comprar_entradas({comprador.UsuarioId}, {evento.EventoId}, {2})");

            async Task Token(Usuarios u)
            {
                var r = await client.PostAsJsonAsync("/api/auth/login", new { email = u.Correo, password = "PruebaTemporal2026!" });
                Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
                var json = await r.Content.ReadFromJsonAsync<JsonElement>();
                client.DefaultRequestHeaders.Authorization = new("Bearer", json.GetProperty("accessToken").GetString());
            }
            await Token(org);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/events", new { categoriaId = categoria,
                nombre = "PRUEBA_ROLLBACK_SIN_CIUDAD", descripcion = "Validación de ciudad", direccion = "Dirección de prueba",
                fechaInicio = DateTime.UtcNow.AddDays(30), aforo = 10, precio = 0, publicar = false })).StatusCode);
            var crear = await client.PostAsJsonAsync("/api/events", new { categoriaId = categoria,
                nombre = "PRUEBA_ROLLBACK_CREACION", descripcion = "Validación de ciudad", direccion = "Dirección de prueba", ciudad = "Lima",
                fechaInicio = DateTime.UtcNow.AddDays(30), aforo = 10, precio = 0, publicar = false });
            Assert.True(crear.IsSuccessStatusCode, await crear.Content.ReadAsStringAsync());
            var lista = await client.GetAsync($"/api/events/{evento.EventoId}/asistentes");
            Assert.True(lista.IsSuccessStatusCode, await lista.Content.ReadAsStringAsync());
            var asistencia = await lista.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, asistencia.GetProperty("entradasVendidas").GetInt32());
            var entradaId = asistencia.GetProperty("asistentes")[0].GetProperty("entradaId").GetGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT public.fn_validar_checkin({entradaId}, {evento.EventoId}, {org.UsuarioId})");
            asistencia = await client.GetFromJsonAsync<JsonElement>($"/api/events/{evento.EventoId}/asistentes");
            Assert.Equal(1, asistencia.GetProperty("checkInsRealizados").GetInt32());
            await Token(comprador);
            var tokenComprador = client.DefaultRequestHeaders.Authorization;
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/events/{evento.EventoId}/asistentes")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/usuarios")).StatusCode);
            var chat = await client.PostAsJsonAsync("/api/ai/chat", new { mensaje = "¿Qué eventos hay en CIUDAD_PRUEBA_EDUARDO?" });
            Assert.True(chat.IsSuccessStatusCode, await chat.Content.ReadAsStringAsync());
            var respuesta = await chat.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(respuesta.GetProperty("eventos").EnumerateArray().Any(e => e.GetProperty("eventoId").GetInt32() == evento.EventoId),
                await chat.Content.ReadAsStringAsync() + "\n" + string.Join("\n", factory.SalidasIA));
            var conversacion = respuesta.GetProperty("conversacionId").GetInt32();
            chat = await client.PostAsJsonAsync("/api/ai/chat", new { mensaje = "¿Cuántos cupos quedan y en qué fecha será?", conversacionId = conversacion });
            Assert.True(chat.IsSuccessStatusCode, await chat.Content.ReadAsStringAsync());
            respuesta = await chat.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(respuesta.GetProperty("eventos").EnumerateArray().Any(e => e.GetProperty("eventoId").GetInt32() == evento.EventoId && e.GetProperty("cuposDisponibles").GetInt32() == 8),
                await chat.Content.ReadAsStringAsync() + "\n" + string.Join("\n", factory.SalidasIA));
            Assert.Equal(4, await db.MensajesChatbot.CountAsync(m => m.ConversacionId == conversacion));
            Assert.Equal(1, await db.EventosEmbeddings.CountAsync(e => e.EventoId == evento.EventoId && e.Vigente));
            // La primera pregunta fue general: el seguimiento debe usar el ID de evento
            // almacenado, incluso después de responder que un presupuesto no alcanza.
            foreach (var (monto, frase, cantidad) in new[] { (24, "te falta S/ 1.00", 0), (25, "alcanza exactamente", 1), (30, "te sobran S/ 5.00", 1) })
            {
                chat = await client.PostAsJsonAsync("/api/ai/chat", new { mensaje = $"Ahora mi presupuesto máximo es de {monto} soles. ¿Puedo pagar ese evento?", conversacionId = conversacion });
                Assert.True(chat.IsSuccessStatusCode, await chat.Content.ReadAsStringAsync());
                respuesta = await chat.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Contains(frase, respuesta.GetProperty("respuesta").GetString());
                Assert.Equal(cantidad, respuesta.GetProperty("eventos").GetArrayLength());
            }
            await Token(org);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/ai/chat", new { mensaje = "Ver conversación ajena", conversacionId = conversacion })).StatusCode);

            await Token(admin);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/admin/eventos/{evento.EventoId}/moderar", new { accion = "Suspender", motivo = "Prueba con rollback", confirmado = false })).StatusCode);
            var moderar = await client.PatchAsJsonAsync($"/api/admin/eventos/{evento.EventoId}/moderar", new { accion = "Suspender", motivo = "Prueba con rollback", confirmado = true });
            Assert.Equal(HttpStatusCode.NoContent, moderar.StatusCode);
            await db.Entry(evento).ReloadAsync(); Assert.Equal("Suspendido", evento.EstadoModeracion);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/events/{evento.EventoId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/catalog/events/{evento.EventoId}")).StatusCode);
            Assert.False(await db.EventosEmbeddings.AnyAsync(e => e.EventoId == evento.EventoId && e.Vigente));
            moderar = await client.PatchAsJsonAsync($"/api/admin/eventos/{evento.EventoId}/moderar", new { accion = "Retirar", motivo = "Prueba con rollback", confirmado = true });
            Assert.Equal(HttpStatusCode.NoContent, moderar.StatusCode);
            await db.Entry(evento).ReloadAsync(); Assert.Equal("Retirado", evento.EstadoModeracion);
            Assert.Equal(2, await db.AccionesModeracion.CountAsync(a => a.AdministradorId == admin.UsuarioId && a.EntidadTipo == "Evento"));
            Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/admin/usuarios/{comprador.UsuarioId}/estado", new { estado = "Bloqueado", motivo = "Prueba con rollback", confirmado = true })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = comprador.Correo, password = "PruebaTemporal2026!" })).StatusCode);
            var tokenAdmin = client.DefaultRequestHeaders.Authorization;
            client.DefaultRequestHeaders.Authorization = tokenComprador;
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/ai/chat", new { mensaje = "Hola" })).StatusCode);
            client.DefaultRequestHeaders.Authorization = tokenAdmin;
            Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/admin/usuarios/{comprador.UsuarioId}/estado", new { estado = "Activo", motivo = "Prueba con rollback", confirmado = true })).StatusCode);
            Assert.Equal(4, await db.AccionesModeracion.CountAsync(a => a.AdministradorId == admin.UsuarioId));
        }
        finally { await tx.RollbackAsync(); }
    }
}

public sealed class ModeloObservado(IModeloIA modelo, List<string> salidas) : IModeloIA
{
    public string ModeloEmbeddings => modelo.ModeloEmbeddings;
    public Task<float[]> Embed(string texto, CancellationToken ct) => modelo.Embed(texto, ct);
    public async Task<string> Responder(IReadOnlyList<TurnoChat> mensajes, CancellationToken ct)
    {
        var salida = await modelo.Responder(mensajes, ct); salidas.Add(salida); return salida;
    }
}

public sealed class SupabaseFactAttribute : FactAttribute
{
    public SupabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EVENTOS_TEST_CONNECTION"))) Skip = "Requiere EVENTOS_TEST_CONNECTION con PostgreSQL; los datos se revierten.";
    }
}
