using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EventosPeruIA.CORE.Core.Entities;

namespace EventosPeruIA.CORE.Infrastructure.Data;

public partial class PostgresContext : DbContext
{
    public PostgresContext()
    {
    }

    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccionesModeracion> AccionesModeracion { get; set; }
    public virtual DbSet<Categorias> Categorias { get; set; }
    public virtual DbSet<Checkins> Checkins { get; set; }
    public virtual DbSet<Compras> Compras { get; set; }
    public virtual DbSet<ConversacionesChatbot> ConversacionesChatbot { get; set; }
    public virtual DbSet<Entradas> Entradas { get; set; }
    public virtual DbSet<Eventos> Eventos { get; set; }
    public virtual DbSet<EventosCancelacion> EventosCancelacion { get; set; }
    public virtual DbSet<EventosEmbeddings> EventosEmbeddings { get; set; }
    public virtual DbSet<Faqs> Faqs { get; set; }
    public virtual DbSet<MensajesChatbot> MensajesChatbot { get; set; }
    public virtual DbSet<MotivosCancelacion> MotivosCancelacion { get; set; }
    public virtual DbSet<Notificaciones> Notificaciones { get; set; }
    public virtual DbSet<ParametrosSistema> ParametrosSistema { get; set; }
    public virtual DbSet<Resenas> Resenas { get; set; }
    public virtual DbSet<TokensRecuperacionContrasena> TokensRecuperacionContrasena { get; set; }
    public virtual DbSet<UsuarioEventoInteraccion> UsuarioEventoInteraccion { get; set; }
    public virtual DbSet<UsuarioIntereses> UsuarioIntereses { get; set; }
    public virtual DbSet<Usuarios> Usuarios { get; set; }

    // Vistas SQL
    public virtual DbSet<VwAsistenciaPorEvento> VwAsistenciaPorEvento { get; set; }
    public virtual DbSet<VwAsistentesEvento> VwAsistentesEvento { get; set; }
    public virtual DbSet<VwDisponibilidadEventos> VwDisponibilidadEventos { get; set; }
    public virtual DbSet<VwEmbeddingsRecomendables> VwEmbeddingsRecomendables { get; set; }
    public virtual DbSet<VwEventosPendientesEmbedding> VwEventosPendientesEmbedding { get; set; }
    public virtual DbSet<VwEventosPublicos> VwEventosPublicos { get; set; }
    public virtual DbSet<VwMisEntradas> VwMisEntradas { get; set; }
    public virtual DbSet<VwPromedioResenasEvento> VwPromedioResenasEvento { get; set; }
    public virtual DbSet<VwResenasEvento> VwResenasEvento { get; set; }
    public virtual DbSet<VwVentasPorEvento> VwVentasPorEvento { get; set; }
    public virtual DbSet<VwVentasPorFecha> VwVentasPorFecha { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccionesModeracion>(entity =>
        {
            entity.HasKey(e => e.AccionId).HasName("pk_acciones_moderacion");

            entity.ToTable("acciones_moderacion", tb => tb.HasComment("Auditoría inmutable de acciones de administradores (US-016). No se edita ni se borra."));

            entity.HasIndex(e => e.AdministradorId, "ix_acciones_moderacion_admin");

            entity.HasIndex(e => new { e.EntidadTipo, e.EntidadId }, "ix_acciones_moderacion_ent");

            entity.HasIndex(e => e.FechaHora, "ix_acciones_moderacion_fecha").IsDescending();

            entity.Property(e => e.AccionId).HasColumnName("accion_id");
            entity.Property(e => e.AdministradorId).HasColumnName("administrador_id");
            entity.Property(e => e.EntidadId)
                .HasComment("Id del usuario, evento o reseña según entidad_tipo.")
                .HasColumnName("entidad_id");
            entity.Property(e => e.EntidadTipo)
                .HasMaxLength(20)
                .HasColumnName("entidad_tipo");
            entity.Property(e => e.EstadoAnterior)
                .HasMaxLength(30)
                .HasColumnName("estado_anterior");
            entity.Property(e => e.EstadoNuevo)
                .HasMaxLength(30)
                .HasColumnName("estado_nuevo");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.IpOrigen).HasColumnName("ip_origen");
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .HasColumnName("motivo");
            entity.Property(e => e.TipoAccion)
                .HasMaxLength(30)
                .HasColumnName("tipo_accion");

            entity.HasOne(d => d.Administrador).WithMany(p => p.AccionesModeracion)
                .HasForeignKey(d => d.AdministradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_acciones_moderacion_admin");
        });

        modelBuilder.Entity<Categorias>(entity =>
        {
            entity.HasKey(e => e.CategoriaId).HasName("pk_categorias");

            entity.ToTable("categorias", tb => tb.HasComment("Catálogo de categorías de eventos (conciertos, talleres, ferias, charlas, deportes)."));

            entity.HasIndex(e => e.Nombre, "uq_categorias_nombre").IsUnique();

            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Icono)
                .HasMaxLength(50)
                .HasColumnName("icono");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .HasColumnName("nombre");
            entity.Property(e => e.Orden).HasColumnName("orden");
        });

        modelBuilder.Entity<Checkins>(entity =>
        {
            entity.HasKey(e => e.CheckInId).HasName("pk_checkins");

            entity.ToTable("checkins", tb => tb.HasComment("Cada intento de validación de una entrada el día del evento, aprobado o rechazado."));

            entity.HasIndex(e => e.EntradaId, "ix_checkins_entrada");

            entity.HasIndex(e => new { e.EventoId, e.FechaHora }, "ix_checkins_evento").IsDescending(false, true);

            entity.HasIndex(e => e.EntradaId, "ux_checkins_entrada_aprobada")
                .IsUnique()
                .HasFilter("((resultado)::text = 'Aprobado'::text)");

            entity.Property(e => e.CheckInId).HasColumnName("check_in_id");
            entity.Property(e => e.Dispositivo)
                .HasMaxLength(150)
                .HasColumnName("dispositivo");
            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.IpOrigen).HasColumnName("ip_origen");
            entity.Property(e => e.MetodoIngreso)
                .HasMaxLength(20)
                .HasColumnName("metodo_ingreso");
            entity.Property(e => e.MotivoRechazo)
                .HasMaxLength(200)
                .HasComment("Obligatorio cuando el resultado es Rechazado.")
                .HasColumnName("motivo_rechazo");
            entity.Property(e => e.Resultado)
                .HasMaxLength(20)
                .HasColumnName("resultado");
            entity.Property(e => e.ValidadoPorId).HasColumnName("validado_por_id");

            entity.HasOne(d => d.Entrada).WithOne(p => p.Checkins)
                .HasForeignKey<Checkins>(d => d.EntradaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_checkins_entrada");

            entity.HasOne(d => d.Evento).WithMany(p => p.Checkins)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_checkins_evento");

            entity.HasOne(d => d.ValidadoPor).WithMany(p => p.Checkins)
                .HasForeignKey(d => d.ValidadoPorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_checkins_validador");
        });

        modelBuilder.Entity<Compras>(entity =>
        {
            entity.HasKey(e => e.CompraId).HasName("pk_compras");

            entity.ToTable("compras", tb => tb.HasComment("Una compra agrupa una o más entradas de un mismo evento. Historial: no se borra."));

            entity.HasIndex(e => e.CompradorId, "ix_compras_comprador");

            entity.HasIndex(e => e.EventoId, "ix_compras_evento");

            entity.HasIndex(e => new { e.CompraId, e.EventoId, e.CompradorId }, "uq_compras_compra_evento_comprador").IsUnique();

            entity.HasIndex(e => e.CodigoTransaccion, "ux_compras_codigo_transaccion")
                .IsUnique()
                .HasFilter("(codigo_transaccion IS NOT NULL)");

            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.CantidadEntradas).HasColumnName("cantidad_entradas");
            entity.Property(e => e.CodigoTransaccion)
                .HasMaxLength(50)
                .HasColumnName("codigo_transaccion");
            entity.Property(e => e.CompradorId).HasColumnName("comprador_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Confirmada'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaAnulacion).HasColumnName("fecha_anulacion");
            entity.Property(e => e.FechaCompra)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_compra");
            entity.Property(e => e.FechaReembolso).HasColumnName("fecha_reembolso");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Simulado'::character varying")
                .HasComment("Simulado mientras no exista pasarela de pagos real.")
                .HasColumnName("metodo_pago");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .HasDefaultValueSql("'PEN'::bpchar")
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.MontoTotal)
                .HasPrecision(10, 2)
                .HasColumnName("monto_total");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasComment("Precio del evento al momento de comprar; el monto_total siempre es precio_unitario x cantidad.")
                .HasColumnName("precio_unitario");

            entity.HasOne(d => d.Comprador).WithMany(p => p.Compras)
                .HasForeignKey(d => d.CompradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_compras_comprador");

            entity.HasOne(d => d.Evento).WithMany(p => p.Compras)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_compras_evento");
        });

        modelBuilder.Entity<ConversacionesChatbot>(entity =>
        {
            entity.HasKey(e => e.ConversacionId).HasName("pk_conversaciones_chatbot");

            entity.ToTable("conversaciones_chatbot", tb => tb.HasComment("Conversaciones de un usuario con el asistente de IA (US-017)."));

            entity.HasIndex(e => e.UsuarioId, "ix_conversaciones_usuario");

            entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.FechaInicio)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.Titulo)
                .HasMaxLength(150)
                .HasColumnName("titulo");
            entity.Property(e => e.UltimoMensajeEn).HasColumnName("ultimo_mensaje_en");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ConversacionesChatbot)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_conversaciones_usuario");
        });

        modelBuilder.Entity<Entradas>(entity =>
        {
            entity.HasKey(e => e.EntradaId).HasName("pk_entradas");

            entity.ToTable("entradas", tb => tb.HasComment("Entradas individuales con QR único (UUID). Estados: Valida, Utilizada, Anulada. Historial: no se borra."));

            entity.HasIndex(e => e.CompraId, "ix_entradas_compra");

            entity.HasIndex(e => new { e.CompradorId, e.Estado }, "ix_entradas_comprador_estado");

            entity.HasIndex(e => new { e.EventoId, e.Estado }, "ix_entradas_evento_estado");

            entity.HasIndex(e => new { e.EntradaId, e.CompradorId, e.EventoId }, "uq_entradas_entrada_comprador_evento").IsUnique();

            entity.HasIndex(e => new { e.EventoId, e.CodigoCorto }, "ux_entradas_evento_codigo").IsUnique();

            entity.Property(e => e.EntradaId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasComment("UUID generado por el servidor (RN-04). Es lo que contiene el código QR.")
                .HasColumnName("entrada_id");
            entity.Property(e => e.CodigoCorto)
                .HasMaxLength(16)
                .HasComputedColumnSql("('TCK-'::text || upper(\"left\"(encode(uuid_send(entrada_id), 'hex'::text), 10)))", true)
                .HasComment("Referencia legible TCK-XXXXXXXXXX derivada del UUID; única por evento. Sirve para el ingreso manual.")
                .HasColumnName("codigo_corto");
            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.CompradorId).HasColumnName("comprador_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Valida'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.EstadoReembolso)
                .HasMaxLength(20)
                .HasComment("PendienteReembolso solo en entradas Anuladas de eventos de pago.")
                .HasColumnName("estado_reembolso");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaAnulacion).HasColumnName("fecha_anulacion");
            entity.Property(e => e.FechaEmision)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_emision");
            entity.Property(e => e.FechaReembolso).HasColumnName("fecha_reembolso");
            entity.Property(e => e.FechaUso).HasColumnName("fecha_uso");
            entity.Property(e => e.PrecioPagado)
                .HasPrecision(10, 2)
                .HasColumnName("precio_pagado");
            entity.Property(e => e.TitularNombre)
                .HasMaxLength(201)
                .HasComment("Nombre del titular que se imprime en el voucher (copia del comprador al comprar).")
                .HasColumnName("titular_nombre");

            entity.HasOne(d => d.Comprador).WithMany(p => p.Entradas)
                .HasForeignKey(d => d.CompradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_entradas_comprador");

            entity.HasOne(d => d.Evento).WithMany(p => p.Entradas)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_entradas_evento");

            entity.HasOne(d => d.Compras).WithMany(p => p.Entradas)
                .HasPrincipalKey(p => new { p.CompraId, p.EventoId, p.CompradorId })
                .HasForeignKey(d => new { d.CompraId, d.EventoId, d.CompradorId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_entradas_compra");
        });

        modelBuilder.Entity<Eventos>(entity =>
        {
            entity.HasKey(e => e.EventoId).HasName("pk_eventos");

            entity.ToTable("eventos", tb => tb.HasComment("Eventos publicados por organizadores. estado = ciclo de vida; estado_moderacion = decisión del administrador."));

            entity.HasIndex(e => e.Busqueda, "ix_eventos_busqueda").HasMethod("gin");

            entity.HasIndex(e => new { e.Estado, e.EstadoModeracion, e.Ciudad, e.FechaInicio }, "ix_eventos_catalogo");

            entity.HasIndex(e => e.CategoriaId, "ix_eventos_categoria");

            entity.HasIndex(e => new { e.Estado, e.FechaInicio }, "ix_eventos_estado_fecha");

            entity.HasIndex(e => e.OrganizadorId, "ix_eventos_organizador");

            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.Aforo).HasColumnName("aforo");
            entity.Property(e => e.Busqueda)
                .HasComputedColumnSql("to_tsvector('spanish'::regconfig, fn_unaccent((((COALESCE(nombre, ''::character varying))::text || ' '::text) || COALESCE(descripcion, ''::text))))", true)
                .HasComment("Índice de texto completo (español, sin tildes) sobre nombre y descripción.")
                .HasColumnName("busqueda");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .HasColumnName("direccion");
            entity.Property(e => e.EsGratuito)
                .HasComputedColumnSql("(precio = (0)::numeric)", true)
                .HasColumnName("es_gratuito");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Borrador'::character varying")
                .HasComment("Borrador, Publicado, Finalizado o Cancelado (RN-06).")
                .HasColumnName("estado");
            entity.Property(e => e.EstadoModeracion)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Aprobado'::character varying")
                .HasComment("PendienteRevision, Aprobado, Rechazado, Suspendido o Retirado. Solo Aprobado se vende.")
                .HasColumnName("estado_moderacion");
            entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaModeracion).HasColumnName("fecha_moderacion");
            entity.Property(e => e.FechaPublicacion).HasColumnName("fecha_publicacion");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.Latitud)
                .HasPrecision(9, 6)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(9, 6)
                .HasColumnName("longitud");
            entity.Property(e => e.MaxEntradasPorCompra)
                .HasComment("Tope de entradas por compra para este evento. Nulo = usa el parámetro global.")
                .HasColumnName("max_entradas_por_compra");
            entity.Property(e => e.ModeradoPorId).HasColumnName("moderado_por_id");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .HasDefaultValueSql("'PEN'::bpchar")
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.MotivoModeracion)
                .HasMaxLength(300)
                .HasColumnName("motivo_moderacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.NombreLugar)
                .HasMaxLength(150)
                .HasColumnName("nombre_lugar");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.VersionFila)
                .HasDefaultValue(1L)
                .HasComment("Se incrementa en cada UPDATE (concurrencia optimista en EF Core).")
                .HasColumnName("version_fila");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eventos_categoria");

            entity.HasOne(d => d.ModeradoPor).WithMany(p => p.EventosModeradoPor)
                .HasForeignKey(d => d.ModeradoPorId)
                .HasConstraintName("fk_eventos_moderador");

            entity.HasOne(d => d.Organizador).WithMany(p => p.EventosOrganizador)
                .HasForeignKey(d => d.OrganizadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eventos_organizador");
        });

        modelBuilder.Entity<EventosCancelacion>(entity =>
        {
            entity.HasKey(e => e.EventoCancelacionId).HasName("pk_eventos_cancelacion");

            entity.ToTable("eventos_cancelacion", tb => tb.HasComment("Registro de la cancelación de un evento: quién, cuándo, por qué y cuántas entradas se anularon."));

            entity.HasIndex(e => e.EventoId, "uq_eventos_cancelacion_evento").IsUnique();

            entity.Property(e => e.EventoCancelacionId).HasColumnName("evento_cancelacion_id");
            entity.Property(e => e.CanceladoPorId).HasColumnName("cancelado_por_id");
            entity.Property(e => e.CantidadEntradasAfectadas).HasColumnName("cantidad_entradas_afectadas");
            entity.Property(e => e.DescripcionAdicional)
                .HasMaxLength(500)
                .HasColumnName("descripcion_adicional");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.MontoReembolsable)
                .HasPrecision(12, 2)
                .HasColumnName("monto_reembolsable");
            entity.Property(e => e.MotivoCancelacionId).HasColumnName("motivo_cancelacion_id");

            entity.HasOne(d => d.CanceladoPor).WithMany(p => p.EventosCancelacion)
                .HasForeignKey(d => d.CanceladoPorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eventos_cancelacion_usuario");

            entity.HasOne(d => d.Evento).WithOne(p => p.EventosCancelacion)
                .HasForeignKey<EventosCancelacion>(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eventos_cancelacion_evento");

            entity.HasOne(d => d.MotivoCancelacion).WithMany(p => p.EventosCancelacion)
                .HasForeignKey(d => d.MotivoCancelacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eventos_cancelacion_motivo");
        });

        modelBuilder.Entity<EventosEmbeddings>(entity =>
        {
            entity.HasKey(e => e.EmbeddingId).HasName("pk_eventos_embeddings");

            entity.ToTable("eventos_embeddings", tb => tb.HasComment("Vectores de los eventos para el chatbot (RAG). Un solo embedding vigente por evento; el resto es historial."));

            entity.HasIndex(e => e.EventoId, "ix_embeddings_evento");

            entity.HasIndex(e => e.EventoId, "ux_embeddings_vigente")
                .IsUnique()
                .HasFilter("vigente");

            entity.Property(e => e.EmbeddingId).HasColumnName("embedding_id");
            entity.Property(e => e.ContenidoHash)
                .HasMaxLength(32)
                .HasComputedColumnSql("md5(contenido_texto)", true)
                .HasComment("md5 del texto indexado; si cambia el contenido del evento deja de coincidir y hay que reindexar.")
                .HasColumnName("contenido_hash");
            entity.Property(e => e.ContenidoTexto).HasColumnName("contenido_texto");
            entity.Property(e => e.Dimensiones)
                .HasComputedColumnSql("(cardinality(vector_embedding))::smallint", true)
                .HasColumnName("dimensiones");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_generacion");
            entity.Property(e => e.FechaInvalidacion).HasColumnName("fecha_invalidacion");
            entity.Property(e => e.ModeloUsado)
                .HasMaxLength(100)
                .HasColumnName("modelo_usado");
            entity.Property(e => e.VectorEmbedding).HasColumnName("vector_embedding");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version");
            entity.Property(e => e.Vigente)
                .HasDefaultValue(true)
                .HasComment("Solo una versión vigente por evento (índice único parcial).")
                .HasColumnName("vigente");

            entity.HasOne(d => d.Evento).WithOne(p => p.EventosEmbeddings)
                .HasForeignKey<EventosEmbeddings>(d => d.EventoId)
                .HasConstraintName("fk_eventos_embeddings_evento");
        });

        modelBuilder.Entity<Faqs>(entity =>
        {
            entity.HasKey(e => e.FaqId).HasName("pk_faqs");

            entity.ToTable("faqs", tb => tb.HasComment("Preguntas frecuentes: base de conocimiento del chatbot (RAG sobre FAQs)."));

            entity.HasIndex(e => new { e.Activo, e.Orden }, "ix_faqs_activas");

            entity.HasIndex(e => e.Pregunta, "uq_faqs_pregunta").IsUnique();

            entity.Property(e => e.FaqId).HasColumnName("faq_id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Categoria)
                .HasMaxLength(60)
                .HasDefaultValueSql("'General'::character varying")
                .HasColumnName("categoria");
            entity.Property(e => e.ContenidoHash)
                .HasMaxLength(32)
                .HasComputedColumnSql("md5((((pregunta)::text || '\n'::text) || respuesta))", true)
                .HasColumnName("contenido_hash");
            entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaEmbedding).HasColumnName("fecha_embedding");
            entity.Property(e => e.ModeloEmbedding)
                .HasMaxLength(100)
                .HasColumnName("modelo_embedding");
            entity.Property(e => e.Orden).HasColumnName("orden");
            entity.Property(e => e.Pregunta)
                .HasMaxLength(300)
                .HasColumnName("pregunta");
            entity.Property(e => e.Respuesta).HasColumnName("respuesta");
            entity.Property(e => e.VectorEmbedding).HasColumnName("vector_embedding");
        });

        modelBuilder.Entity<MensajesChatbot>(entity =>
        {
            entity.HasKey(e => e.MensajeId).HasName("pk_mensajes_chatbot");

            entity.ToTable("mensajes_chatbot", tb => tb.HasComment("Mensajes de una conversación, con las fuentes (eventos y FAQs) usadas para responder."));

            entity.HasIndex(e => new { e.ConversacionId, e.FechaHora }, "ix_mensajes_conversacion");

            entity.Property(e => e.MensajeId).HasColumnName("mensaje_id");
            entity.Property(e => e.Contenido).HasColumnName("contenido");
            entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
            entity.Property(e => e.Emisor)
                .HasMaxLength(15)
                .HasColumnName("emisor");
            entity.Property(e => e.EventosReferenciados).HasColumnName("eventos_referenciados");
            entity.Property(e => e.FaqsReferenciadas).HasColumnName("faqs_referenciadas");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.ModeloUsado)
                .HasMaxLength(100)
                .HasColumnName("modelo_usado");
            entity.Property(e => e.TokensEntrada).HasColumnName("tokens_entrada");
            entity.Property(e => e.TokensSalida).HasColumnName("tokens_salida");

            entity.HasOne(d => d.Conversacion).WithMany(p => p.MensajesChatbot)
                .HasForeignKey(d => d.ConversacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_mensajes_conversacion");
        });

        modelBuilder.Entity<MotivosCancelacion>(entity =>
        {
            entity.HasKey(e => e.MotivoCancelacionId).HasName("pk_motivos_cancelacion");

            entity.ToTable("motivos_cancelacion", tb => tb.HasComment("Catálogo de motivos válidos para cancelar un evento (US-009)."));

            entity.HasIndex(e => e.Codigo, "uq_motivos_cancelacion_codigo").IsUnique();

            entity.Property(e => e.MotivoCancelacionId).HasColumnName("motivo_cancelacion_id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(40)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.HasKey(e => e.NotificacionId).HasName("pk_notificaciones");

            entity.ToTable("notificaciones", tb => tb.HasComment("Avisos para el usuario: compra, recordatorio, cambios, cancelación y moderación (US-014)."));

            entity.HasIndex(e => new { e.UsuarioId, e.Leida, e.FechaCreacion }, "ix_notificaciones_usuario").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.UsuarioId, e.EventoId }, "ux_notificaciones_recordatorio")
                .IsUnique()
                .HasFilter("((tipo)::text = 'Recordatorio'::text)");

            entity.Property(e => e.NotificacionId).HasColumnName("notificacion_id");
            entity.Property(e => e.Datos)
                .HasColumnType("jsonb")
                .HasColumnName("datos");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaLectura).HasColumnName("fecha_lectura");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(500)
                .HasColumnName("mensaje");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");
            entity.Property(e => e.Titulo)
                .HasMaxLength(150)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("fk_notificaciones_evento");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notificaciones_usuario");
        });

        modelBuilder.Entity<ParametrosSistema>(entity =>
        {
            entity.HasKey(e => e.Clave).HasName("pk_parametros_sistema");

            entity.ToTable("parametros_sistema", tb => tb.HasComment("Reglas de negocio configurables (duraciones, tiempos, límites). Ver columna descripcion."));

            entity.Property(e => e.Clave)
                .HasMaxLength(60)
                .HasColumnName("clave");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaActualizacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_actualizacion");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .HasDefaultValueSql("'entero'::character varying")
                .HasColumnName("tipo");
            entity.Property(e => e.Valor)
                .HasMaxLength(200)
                .HasColumnName("valor");
        });

        modelBuilder.Entity<Resenas>(entity =>
        {
            entity.HasKey(e => e.ResenaId).HasName("pk_resenas");

            entity.ToTable("resenas", tb => tb.HasComment("Calificaciones de asistentes que usaron su entrada en un evento finalizado (US-013)."));

            entity.HasIndex(e => e.EventoId, "ix_resenas_evento");

            entity.HasIndex(e => new { e.UsuarioId, e.EventoId }, "ux_resenas_usuario_evento_activa")
                .IsUnique()
                .HasFilter("((estado)::text = 'Activa'::text)");

            entity.Property(e => e.ResenaId).HasColumnName("resena_id");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.Comentario)
                .HasMaxLength(1000)
                .HasColumnName("comentario");
            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activa'::character varying")
                .HasComment("Activa o Moderada (oculta). Solo cuenta la reseña activa por usuario y evento.")
                .HasColumnName("estado");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaModeracion).HasColumnName("fecha_moderacion");
            entity.Property(e => e.ModeradaPorId).HasColumnName("moderada_por_id");
            entity.Property(e => e.MotivoModeracion)
                .HasMaxLength(300)
                .HasColumnName("motivo_moderacion");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Resenas)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_resenas_evento");

            entity.HasOne(d => d.ModeradaPor).WithMany(p => p.ResenasModeradaPor)
                .HasForeignKey(d => d.ModeradaPorId)
                .HasConstraintName("fk_resenas_moderador");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ResenasUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_resenas_usuario");

            entity.HasOne(d => d.Entradas).WithMany(p => p.Resenas)
                .HasPrincipalKey(p => new { p.EntradaId, p.CompradorId, p.EventoId })
                .HasForeignKey(d => new { d.EntradaId, d.UsuarioId, d.EventoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_resenas_entrada");
        });

        modelBuilder.Entity<TokensRecuperacionContrasena>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("pk_tokens_recuperacion");

            entity.ToTable("tokens_recuperacion_contrasena", tb => tb.HasComment("Tokens de un solo uso para recuperar contraseña (US-018). Solo se guarda el hash."));

            entity.HasIndex(e => e.UsuarioId, "ix_tokens_usuario");

            entity.HasIndex(e => e.TokenHash, "ux_tokens_hash").IsUnique();

            entity.Property(e => e.TokenId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("token_id");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaExpiracion).HasColumnName("fecha_expiracion");
            entity.Property(e => e.FechaUso).HasColumnName("fecha_uso");
            entity.Property(e => e.IpSolicitud).HasColumnName("ip_solicitud");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Utilizado).HasColumnName("utilizado");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TokensRecuperacionContrasena)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("fk_tokens_usuario");
        });

        modelBuilder.Entity<UsuarioEventoInteraccion>(entity =>
        {
            entity.HasKey(e => e.InteraccionId).HasName("pk_usuario_evento_interaccion");

            entity.ToTable("usuario_evento_interaccion", tb => tb.HasComment("Vistas, búsquedas, compras y favoritos del usuario: señales para las recomendaciones (US-007)."));

            entity.HasIndex(e => e.EventoId, "ix_interaccion_evento");

            entity.HasIndex(e => new { e.UsuarioId, e.FechaHora }, "ix_interaccion_usuario").IsDescending(false, true);

            entity.Property(e => e.InteraccionId).HasColumnName("interaccion_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.TerminoBusqueda)
                .HasMaxLength(200)
                .HasColumnName("termino_busqueda");
            entity.Property(e => e.TipoInteraccion)
                .HasMaxLength(20)
                .HasColumnName("tipo_interaccion");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.UsuarioEventoInteraccion)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("fk_interaccion_evento");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuarioEventoInteraccion)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_interaccion_usuario");
        });

        modelBuilder.Entity<UsuarioIntereses>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.CategoriaId }).HasName("pk_usuario_intereses");

            entity.ToTable("usuario_intereses", tb => tb.HasComment("Categorías de interés declaradas por el usuario (US-003). Señal explícita de recomendación."));

            entity.HasIndex(e => e.CategoriaId, "ix_intereses_categoria");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");

            entity.HasOne(d => d.Categoria).WithMany(p => p.UsuarioIntereses)
                .HasForeignKey(d => d.CategoriaId)
                .HasConstraintName("fk_usuario_intereses_categoria");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuarioIntereses)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("fk_usuario_intereses_usuario");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("pk_usuarios");

            entity.ToTable("usuarios", tb => tb.HasComment("Cuentas del sistema. Toda cuenta puede comprar y organizar (RN-01); solo es_administrador es especial."));

            entity.HasIndex(e => e.Correo, "uq_usuarios_correo").IsUnique();

            entity.HasIndex(e => new { e.ProveedorAuth, e.IdExterno }, "ux_usuarios_externo")
                .IsUnique()
                .HasFilter("(id_externo IS NOT NULL)");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.BloqueadoHasta)
                .HasComment("Bloqueo temporal por fuerza bruta. Distinto del bloqueo administrativo (estado).")
                .HasColumnName("bloqueado_hasta");
            entity.Property(e => e.CiudadUbicacion)
                .HasMaxLength(100)
                .HasColumnName("ciudad_ubicacion");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(255)
                .HasComment("Hash con sal (bcrypt/argon2/Identity). Nulo si la cuenta solo usa login con Google.")
                .HasColumnName("contrasena_hash");
            entity.Property(e => e.Correo)
                .HasColumnType("citext")
                .HasColumnName("correo");
            entity.Property(e => e.CorreoVerificado).HasColumnName("correo_verificado");
            entity.Property(e => e.EsAdministrador).HasColumnName("es_administrador");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Activo'::character varying")
                .HasComment("Activo, Bloqueado (por un administrador) o Inactivo.")
                .HasColumnName("estado");
            entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion");
            entity.Property(e => e.FechaBloqueo).HasColumnName("fecha_bloqueo");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FechaVerificacion).HasColumnName("fecha_verificacion");
            entity.Property(e => e.FotoUrl)
                .HasMaxLength(500)
                .HasColumnName("foto_url");
            entity.Property(e => e.IdExterno)
                .HasMaxLength(100)
                .HasComment("Identificador del usuario en el proveedor externo (sub de Google).")
                .HasColumnName("id_externo");
            entity.Property(e => e.IntentosFallidos)
                .HasComment("Contador de fallos de inicio de sesión; lo gestiona fn_registrar_intento_login.")
                .HasColumnName("intentos_fallidos");
            entity.Property(e => e.Latitud)
                .HasPrecision(9, 6)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(9, 6)
                .HasColumnName("longitud");
            entity.Property(e => e.MotivoBloqueo)
                .HasMaxLength(300)
                .HasColumnName("motivo_bloqueo");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");
            entity.Property(e => e.ProveedorAuth)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Local'::character varying")
                .HasComment("Local (correo y contraseña) o Google.")
                .HasColumnName("proveedor_auth");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
            entity.Property(e => e.UltimoAcceso).HasColumnName("ultimo_acceso");
        });

        modelBuilder.Entity<VwAsistenciaPorEvento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_asistencia_por_evento")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.AsistentesIngresados).HasColumnName("asistentes_ingresados");
            entity.Property(e => e.EntradasAnuladas).HasColumnName("entradas_anuladas");
            entity.Property(e => e.EntradasVendidas).HasColumnName("entradas_vendidas");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.NombreEvento)
                .HasMaxLength(150)
                .HasColumnName("nombre_evento");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
            entity.Property(e => e.PendientesDeIngreso).HasColumnName("pendientes_de_ingreso");
        });

        modelBuilder.Entity<VwAsistentesEvento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_asistentes_evento")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.CodigoCorto)
                .HasMaxLength(16)
                .HasColumnName("codigo_corto");
            entity.Property(e => e.CompradorId).HasColumnName("comprador_id");
            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.EstadoReembolso)
                .HasMaxLength(20)
                .HasColumnName("estado_reembolso");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision");
            entity.Property(e => e.FechaUso).HasColumnName("fecha_uso");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
            entity.Property(e => e.Titular)
                .HasMaxLength(201)
                .HasColumnName("titular");
        });

        modelBuilder.Entity<VwDisponibilidadEventos>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_disponibilidad_eventos")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.Aforo).HasColumnName("aforo");
            entity.Property(e => e.CuposDisponibles).HasColumnName("cupos_disponibles");
            entity.Property(e => e.EntradasVendidas).HasColumnName("entradas_vendidas");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<VwEmbeddingsRecomendables>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_embeddings_recomendables")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.ContenidoTexto).HasColumnName("contenido_texto");
            entity.Property(e => e.EmbeddingId).HasColumnName("embedding_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaGeneracion).HasColumnName("fecha_generacion");
            entity.Property(e => e.ModeloUsado)
                .HasMaxLength(100)
                .HasColumnName("modelo_usado");
            entity.Property(e => e.VectorEmbedding).HasColumnName("vector_embedding");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<VwEventosPendientesEmbedding>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_eventos_pendientes_embedding")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.EmbeddingVigenteId).HasColumnName("embedding_vigente_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.HashActual)
                .HasMaxLength(32)
                .HasColumnName("hash_actual");
            entity.Property(e => e.HashVigente)
                .HasMaxLength(32)
                .HasColumnName("hash_vigente");
            entity.Property(e => e.Texto).HasColumnName("texto");
        });

        modelBuilder.Entity<VwEventosPublicos>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_eventos_publicos")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.Aforo).HasColumnName("aforo");
            entity.Property(e => e.Agotado).HasColumnName("agotado");
            entity.Property(e => e.Busqueda).HasColumnName("busqueda");
            entity.Property(e => e.Categoria)
                .HasMaxLength(60)
                .HasColumnName("categoria");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.CuposDisponibles).HasColumnName("cupos_disponibles");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .HasColumnName("direccion");
            entity.Property(e => e.EntradasVendidas).HasColumnName("entradas_vendidas");
            entity.Property(e => e.EsGratuito).HasColumnName("es_gratuito");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.Latitud)
                .HasPrecision(9, 6)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(9, 6)
                .HasColumnName("longitud");
            entity.Property(e => e.MaxEntradasPorCompra).HasColumnName("max_entradas_por_compra");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.NombreLugar)
                .HasMaxLength(150)
                .HasColumnName("nombre_lugar");
            entity.Property(e => e.Organizador)
                .HasMaxLength(201)
                .HasColumnName("organizador");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.PromedioCalificacion).HasColumnName("promedio_calificacion");
            entity.Property(e => e.PromedioOrganizador).HasColumnName("promedio_organizador");
            entity.Property(e => e.TotalResenas).HasColumnName("total_resenas");
            entity.Property(e => e.TotalResenasOrganizador).HasColumnName("total_resenas_organizador");
        });

        modelBuilder.Entity<VwMisEntradas>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_mis_entradas")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.CodigoCorto)
                .HasMaxLength(16)
                .HasColumnName("codigo_corto");
            entity.Property(e => e.CompradorId).HasColumnName("comprador_id");
            entity.Property(e => e.DetalleCancelacion)
                .HasMaxLength(500)
                .HasColumnName("detalle_cancelacion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .HasColumnName("direccion");
            entity.Property(e => e.EntradaId).HasColumnName("entrada_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.EstadoEvento)
                .HasMaxLength(20)
                .HasColumnName("estado_evento");
            entity.Property(e => e.EstadoReembolso)
                .HasMaxLength(20)
                .HasColumnName("estado_reembolso");
            entity.Property(e => e.Evento)
                .HasMaxLength(150)
                .HasColumnName("evento");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaUso).HasColumnName("fecha_uso");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.MotivoCancelacion)
                .HasMaxLength(200)
                .HasColumnName("motivo_cancelacion");
            entity.Property(e => e.NombreLugar)
                .HasMaxLength(150)
                .HasColumnName("nombre_lugar");
            entity.Property(e => e.PrecioPagado)
                .HasPrecision(10, 2)
                .HasColumnName("precio_pagado");
            entity.Property(e => e.Titular)
                .HasMaxLength(201)
                .HasColumnName("titular");
        });

        modelBuilder.Entity<VwPromedioResenasEvento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_promedio_resenas_evento")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.PromedioCalificacion).HasColumnName("promedio_calificacion");
            entity.Property(e => e.TotalResenas).HasColumnName("total_resenas");
        });

        modelBuilder.Entity<VwResenasEvento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_resenas_evento")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.Autor)
                .HasMaxLength(110)
                .HasColumnName("autor");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.Comentario)
                .HasMaxLength(1000)
                .HasColumnName("comentario");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.ResenaId).HasColumnName("resena_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
        });

        modelBuilder.Entity<VwVentasPorEvento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ventas_por_evento")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.Aforo).HasColumnName("aforo");
            entity.Property(e => e.EntradasAnuladas).HasColumnName("entradas_anuladas");
            entity.Property(e => e.EntradasDisponibles).HasColumnName("entradas_disponibles");
            entity.Property(e => e.EntradasVendidas).HasColumnName("entradas_vendidas");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasColumnName("estado");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.IngresosConfirmados)
                .HasPrecision(12, 2)
                .HasColumnName("ingresos_confirmados");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("moneda");
            entity.Property(e => e.MontoPendienteReembolso)
                .HasPrecision(12, 2)
                .HasColumnName("monto_pendiente_reembolso");
            entity.Property(e => e.NombreEvento)
                .HasMaxLength(150)
                .HasColumnName("nombre_evento");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
        });

        modelBuilder.Entity<VwVentasPorFecha>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ventas_por_fecha")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.EntradasVendidasEseDia).HasColumnName("entradas_vendidas_ese_dia");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.IngresosEseDia)
                .HasPrecision(12, 2)
                .HasColumnName("ingresos_ese_dia");
            entity.Property(e => e.OrganizadorId).HasColumnName("organizador_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}