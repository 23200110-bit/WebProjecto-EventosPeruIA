# Entrega de Semana 12

## Fuente revisada

Rúbrica proporcionada: proyecto_DAW_2026_2.pdf, cuatro páginas, SHA256
`4c161c77827b04092366d93660e647c5c3da7012d9def2454436fc14e20cf9ec`.
Página 2: ASP.NET Core Web API, SQL Server, Vue.js + Quasar, IA mediante
Microsoft.Extensions.AI. Página 3: Semana 12 (05/11/2026), frontend al 50%
integrado con backend, en GitHub y compartido con integrantes y docente.
Las semanas 15–16 piden el sistema completo, con IA integrada.

Aclaración del docente comunicada por Henry el 28/10/2026: para este proyecto
la base de datos solicitada es **Supabase (PostgreSQL)**. Esta indicación
aclara la mención a SQL Server de la rúbrica escrita.

## Recorte de este avance

| Pantalla | Llamada real a API | Límite declarado |
|---|---|---|
| Inicio y explorar | GET catalog/events y categories | No muestra una lista ficticia si falla la API |
| Filtros combinables | GET catalog/events con query/categoría/ciudad/fechas | Consulta paginada |
| Detalle y aforo | GET catalog/events/{id} cada 15s | Polling; no WebSocket |
| Registro | POST auth/register | Sin Google ni correo de verificación |
| Inicio de sesión | POST auth/login | Token en memoria de pestaña |
| Perfil | GET/PUT profile | Ubicación opcional |
| Recomendaciones | GET recommendations | Reutiliza señales/embeddings existentes |
| Mis entradas y QR | GET my/tickets | Lee entradas existentes; no compra nuevas |
| Reseñas | GET/POST events/{id}/reviews | Sólo entradas utilizadas y evento pasado |
| Avisos | GET my/notifications; POST {id}/read | No genera recordatorios nuevos |

No se equipara diez pantallas con un 50% aprobado. El docente evalúa el
porcentaje con sus criterios. El 29/10/2026 se verificaron catálogo y navegación
contra Supabase real en modo sólo lectura. Sigue pendiente la ejecución en
Windows/equipo del alumno y los flujos reales de escritura autorizados.
La gestión de eventos de PR #23 se conserva aparte, sin sobrescribirla.

## Base de datos confirmada

Supabase (PostgreSQL), según la aclaración de Henry del 28/10/2026 sobre lo
pedido por el profesor. Se conserva el proveedor Npgsql y la arquitectura de
clase. La decisión de tecnología está resuelta; la integración de lectura con la base del
equipo pasó 12 comprobaciones HTTP y 9 de navegador real el 29/10/2026.
No se probó login válido ni escritura en esa corrida. Esta aclaración no aplica migraciones
remotas ni modifica datos, permisos o credenciales.

## Prueba de aceptación reproducible

1. Ejecutar API local con secretos válidos y datos de prueba autorizados.
2. Ejecutar frontend con npm ci y npm run dev.
3. Explorar y combinar filtros; comprobar estado vacío y error sin datos falsos.
4. Registrarse, ingresar, guardar perfil e intereses y consultarlos de nuevo.
5. Abrir detalle y observar que la consulta se repite sin recargar la página.
6. Consultar entradas propias; QR sólo para Válida y evento no cancelado.
7. Leer una notificación; el estado debe persistir sólo en la cuenta dueña.
8. Probar la UI a 390 px y escritorio, sin desbordamiento horizontal.
9. Ejecutar npm run build y dotnet build.
10. Publicar en una rama de GitHub y compartir acceso con el profesor: crear
    código local no acredita publicación ni acceso del docente.

Las pruebas automáticas de ALICE usan Chromium real y controladores/repositorios
.NET reales con EF InMemory en un proceso separado. No prueban PostgreSQL, sus
triggers, RLS, concurrencia de compra, correo o proveedor IA. El laboratorio no
está incorporado al frontend del alumno ni permite activar un modo demo oculto.

## Verificación adicional contra Supabase real (29/10/2026)

31 tests backend aislados PASS, 12 comprobaciones HTTP reales y 9 de navegador
contra la API conectada a Supabase PASS. Se conservaron las 30 relaciones y
los conteos de las 19 tablas, sin insertar fixtures ni cambiar el esquema.
Se ejercieron catálogo, filtros, detalle, reseñas públicas, polling, protección
sin sesión, rechazo de login inexistente y validación local de registro sin envío.
Login válido, registro y demás escrituras reales no se acreditan con esa corrida.
Configuración segura y alcance: [SUPABASE_LOCAL.md](SUPABASE_LOCAL.md).
