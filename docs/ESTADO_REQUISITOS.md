# Matriz de requisitos — fuente: issues públicos del repositorio

Los 19 issues son criterios funcionales; la captura del equipo exige además
DTOs, interfaces, repositorios, servicios y controladores. No se atribuye a esta
matriz el carácter de una rúbrica adicional del profesor.

| Historia | Estado verificable en este incremento | Pendiente para aceptación completa |
|---|---|---|
| US-001 Registro | API, DTO validado, hash y rechazo de duplicados | Formulario web; DB real/constraint concurrente |
| US-002 Login | API JWT, cuentas bloqueadas y validación JWT básica | UI; Google OAuth; recuperación |
| US-003 Perfil | Servicio existente corregido; intereses repetibles; ubicación opcional | UI; persistencia real Supabase |
| US-004 Navegación | No implementada en este backend | Aplicación web adaptable |
| US-005 Búsqueda | API con filtros combinables y paginación | UI, ejecución Npgsql/SQL real |
| US-006 Detalle | API con estado y disponibilidad consultada | UI y actualización reactiva |
| US-007 Recomendaciones | Servicio existente; evita embeddings obsoletos/no finitos o incompatibles | Generación de embeddings reales y UI; pruebas de señal semántica |
| US-008 Gestión eventos | Existe PR #23 ajeno, no mezclado | Revisar/integrar con permiso y probar |
| US-009 Cancelación | Existe PR #23 ajeno, no mezclado | Atomicidad, notificaciones e invalidación IA/QR reales |
| US-010 Compras | No implementado | SQL atómico, aforo, concurrencia y UI |
| US-011 Mis entradas | Entidades/vistas existentes; sin endpoint nuevo | Autorización por dueño, voucher, QR y UI |
| US-012 Check-in | Entidades existentes; sin endpoint nuevo | Validación atómica y rechazo de replay |
| US-013 Reseñas | Servicio existente conservado | UI, moderación; SQL real y concurrencia unicidad |
| US-014 Notificaciones | Entidad existente; sin servicio nuevo | Entrega, recordatorios, lectura por dueño |
| US-015 Reportes | Vistas existentes; sin servicio nuevo | Autorización, filtros, Excel/PDF y UI |
| US-016 Moderación | Login rechaza bloqueo; sin panel nuevo | Acciones admin, auditoría, confirmación y UI |
| US-017 Chatbot/RAG | No implementado | Proveedor IA, recuperación real, sesión, UI |
| US-018 Recuperación | No implementado | Correo autorizado, token hash temporal de un uso y UI |
| US-019 Asistentes | Vista existente; sin servicio nuevo | Filtros por organizador, check-in y UI |

## Dependencias necesarias

- Script SQL original: los modelos no acreditan qué triggers/funciones están
  instalados ni sus firmas. No inventar nombres de funciones sobre Supabase.
- Configuración segura de DB y clave JWT por el equipo; nunca por chat público.
- Google OAuth y correo: proveedores reales y credenciales configuradas por el
  dueño. No simular que funcionan ni activar servicios de pago.
- Confirmación de la rúbrica/fecha de entrega y de la tecnología de frontend si
  el profesor prescribe una. El repositorio actual sólo contiene backend.

## Corrección de alcance — Semana 12

El candidato inicial queda sustituido. Esta versión sigue las capas y
JWT del repositorio del profesor. No se entrega el sistema de rate limit ni de
revalidación de tokens por petición. Las pruebas de ingeniería quedan fuera del
proyecto del alumno. No se requiere aprobar ni publicar la rama anterior.

## Prioridad corregida por la rúbrica de Semana 12

La matriz anterior describía el incremento de backend. El alcance actual de
frontend y sus pendientes está en SEMANA12.md. No se exige implementar las 19
historias al 100% antes del avance del 05/11; no se presenta ese backlog como
si fuera la evaluación ya aprobada de Semana 12.
