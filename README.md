# Eventos Perú IA

Avance de Semana 12: frontend Vue + Quasar integrado con API .NET 10.
La base de datos del proyecto es PostgreSQL en Supabase, conforme a la
indicación del profesor (28/10/2026).

## Arquitectura que pide el profesor

`Controller → IService → Service → IRepository → Repository → PostgresContext`.
Los DTOs son los contratos HTTP; no se exponen entidades de usuarios ni hashes.
- API: controladores, autenticación JWT y OpenAPI.
- CORE/Core: entidades, DTOs, interfaces y reglas de negocio.
- CORE/Infrastructure: consultas y persistencia con EF Core/Npgsql.
- Verificación: pruebas externas; no se agrega otro proyecto o framework de pruebas a la solución de clase.

## Preparación local

1. Instalar SDK .NET 10. No cambiar el proveedor a SQL Server.
2. Abrir `ProyectoFinalWeb-EeventosPeruIA.slnx`.
3. Configurar secretos **fuera de git**, desde la raíz del repositorio:

```powershell
dotnet user-secrets set "ConnectionStrings:DevConnection" "TU_CADENA_POSTGRESQL" --project EventosPeruIA.API
dotnet user-secrets set "JWTSettings:SecretKey" "TU_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES" --project EventosPeruIA.API
dotnet user-secrets set "JWTSettings:Issuer" "EventosPeruIA" --project EventosPeruIA.API
dotnet user-secrets set "JWTSettings:Audience" "EventosPeruIA.Web" --project EventosPeruIA.API
dotnet restore
dotnet build
dotnet run --project EventosPeruIA.API
```

La cadena de conexión debe ser entregada por el administrador del proyecto.
No imprimirla ni enviarla a GitHub. La clave JWT debe generarse con un generador
criptográfico y no reutilizar claves de Supabase. Las variables `JWTSettings__SecretKey`,
`JWTSettings__Issuer`, `JWTSettings__Audience` y `ConnectionStrings__DevConnection` son otra opción.
La API falla con un mensaje de configuración si falta la clave, issuer o audience.
No se crea, reinicia ni migra automáticamente la base remota.

En Development, la especificación está en `/openapi/v1.json`. No se incluye
Swagger UI por defecto. Para probar solicitudes: `EventosPeruIA.API/ProyectoFinalWeb.http`.
En producción usar HTTPS; el login transmite credenciales y no debe exponerse por HTTP.

## Rutas del incremento

| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/auth/register` | Público; validación de datos |
| POST | `/api/auth/login` | Público; verificación de credenciales |
| GET | `/api/catalog/categories` | Público; categorías activas |
| GET | `/api/catalog/events` | Público; filtros y paginación |
| GET | `/api/catalog/events/{id}` | Público; detalle y disponibilidad |
| GET/PUT | `/api/profile` | Token JWT válido |
| GET | `/api/recommendations` | Token JWT válido |
| GET | `/api/events/{id}/reviews` | Público |
| POST | `/api/events/{id}/reviews` | Token JWT válido |

Filtros del catálogo: `query`, `categoryId`, `city`, `from`, `to`, `page`, `pageSize`.
Las fechas se envían ISO 8601 con zona (`2026-11-01T00:00:00-05:00`).
La búsqueda excluye borradores, cancelados, moderación no aprobada y eventos pasados.
El detalle permite mostrar un evento cancelado con su estado, pero no habilita compra.
La disponibilidad se consulta cada vez: este incremento no implementa push reactivo.

El registro crea una cuenta Local, activa, no administradora, con hash salado de
ASP.NET Core Identity. El correo se normaliza y la restricción `citext UNIQUE`
del esquema conserva la unicidad concurrente. El login no devuelve el hash.
Los hashes de otros algoritmos ya existentes requieren compatibilidad explícita;
no se comparan contraseñas en texto plano. JWTSettings configura issuer, audience,
clave y duración (por defecto 30 minutos). La validación JWT sigue el ejemplo
del profesor. El login rechaza cuentas bloqueadas; un token emitido anteriormente
sigue válido hasta su vencimiento. No se agrega un sistema de revocación.

## Alcance y pendientes

Esta versión de Semana 12 **no es la entrega completa de las 19 historias**. Ver
[matriz de requisitos](docs/ESTADO_REQUISITOS.md), con los criterios de los issues
originales en `docs/requisitos-origen.json`. No cerrar issues completos sólo por
estas pruebas: falta la aplicación web y la comprobación con el SQL real.

La rama abierta `feature/gestion-y-cancelacion-de-eventos` / PR #23 contiene trabajo
ajeno de gestión de eventos; no se ha mezclado ni sobrescrito aquí.

## Nivel de Semana 12

Referencia: `luischang/UESAN.ECOMMERCE202602`, commit
`604d59b20d8365d8de9f625475410a3950990c45`.

Se utiliza su estructura de clases normales, constructores con inyección,
DTOs, interfaces, servicios, repositorios, EF y JWT. La configuración JWT se
separa en `JWTSettings`, `IJWTService`, `JWTService` y `ServiceRegistration`.
Se conserva PostgreSQL/Npgsql porque es la base del proyecto del equipo.

No se añade limitador de solicitudes, revalidación de sesión por petición,
mediadores, CQRS, Docker ni microservicios. El frontend usa Vue + Quasar,
porque la rúbrica lo exige, con JavaScript y componentes de Options API. La única
salvaguarda extra frente al ejemplo es no guardar contraseñas en texto plano:
se utiliza el hash integrado de ASP.NET Core, no un sistema propio de cifrado.
Los módulos previos del equipo se conservan sin refactorizarlos de estilo.

## Verificación y límites

Compilar: `dotnet build`. Las pruebas externas verifican servicios,
HTTP y EF InMemory; no se incluyen en el programa que debe explicar el alumno.
Las pruebas InMemory no prueban SQL, constraints, RLS ni triggers de Supabase.
No se ha conectado esta versión a la base real ni se ha migrado su esquema.
Faltan las integraciones y criterios indicados en la matriz; no se declara el
proyecto completo ni Google/correo funcionando.

## Frontend Semana 12 — Vue + Quasar

La rúbrica `proyecto_DAW_2026_2.pdf` fija la presentación de Semana 12 para
el **05/11/2026**: frontend al 50%, integrado al backend y en GitHub. El 100%
corresponde al final del curso. Ver `docs/SEMANA12.md`.

1. Configurar secretos de la API como se explica arriba.
2. En una terminal: `dotnet run --project EventosPeruIA.API --urls http://localhost:5080`.
3. En otra terminal:

```sh
cd EventosPeruIA.Web
npm ci
npm run dev
```

Abrir la URL local que imprime Vite (normalmente http://127.0.0.1:5173).
El proxy `/api` apunta por defecto a http://127.0.0.1:5080. Para otro puerto,
fijar `API_PROXY_TARGET` antes de iniciar Vite. Es una configuración sólo del
servidor de desarrollo: no expone claves en el navegador ni modifica CORS.
Para compilar frontend: `npm run build`. No incluir `node_modules/` ni `dist/`.
Una publicación de producción necesita servir `dist/` y configurar el proxy
`/api` al servidor .NET; `vite preview` por sí solo no publica la API.

Pantallas conectadas: inicio/explorar, búsqueda, detalle con consulta de cupos
cada 15s, registro, login, perfil/intereses, recomendaciones, entradas/QR y
notificaciones. Login conserva el token sólo en memoria; al recargar o cerrar
la pestaña hay que iniciar sesión otra vez. Reseñas se habilitan tras finalizar
el evento y el backend exige una entrada utilizada.

No se procesan compras ni pagos en este avance; el botón aparece deshabilitado
y lo indica. Tampoco se ofrece un Google OAuth o correo ficticio. Los datos de
la UI siempre salen de la API configurada, sin una lista de demostración
incorporada al producto. El laboratorio de pruebas tiene sus propios fixtures
y no debe usarse como base del equipo.

