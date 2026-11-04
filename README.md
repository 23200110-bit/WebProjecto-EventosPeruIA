# Eventos Perú IA

Avance de Semana 12: frontend Vue + Quasar integrado con API .NET 10.
La base de datos del proyecto es PostgreSQL en Supabase.

## Arquitectura

`Controller → IService → Service → IRepository → Repository → PostgresContext`.
Los DTOs son los contratos HTTP; no se exponen entidades de usuarios ni hashes.
- API: controladores, autenticación JWT y OpenAPI.
- CORE/Core: entidades, DTOs, interfaces y reglas de negocio.
- CORE/Infrastructure: consultas y persistencia con EF Core/Npgsql.

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
del profesor. El login rechaza cuentas bloqueadas; cada solicitud autenticada
consulta el estado y el rol actuales de la cuenta para impedir el uso de tokens de cuentas bloqueadas.

## Backend de moderación, asistentes y chat (US-016, US-019, US-017)

| Método | Ruta | Acceso |
|---|---|---|
| GET | `/api/admin/usuarios` | Administrador; búsqueda, estado y paginación |
| GET | `/api/admin/eventos` | Administrador; búsqueda, estado y paginación |
| PATCH | `/api/admin/usuarios/{id}/estado` | Administrador; estado, motivo y confirmación |
| PATCH | `/api/admin/eventos/{id}/moderar` | Administrador; acción, motivo y confirmación |
| GET | `/api/events/{id}/asistentes` | Únicamente el organizador propietario |
| POST | `/api/ai/chat` | Usuario autenticado; conversación privada |

Las consultas a PostgreSQL se implementan en los repositorios; los servicios
aplican reglas de negocio y los controladores reciben y devuelven DTOs. Se utiliza
el `PostgresContext` y las funciones de moderación y embeddings existentes, sin migraciones.

Para el chat, instalar en Ollama los modelos `llama3.2` y `nomic-embed-text`.
La dirección predeterminada es `http://localhost:11434/`; se puede sobrescribir
con `AI__Ollama__BaseUrl`, `AI__Ollama__ChatModel` y `AI__Ollama__EmbeddingModel`.
El body admite `mensaje` y, para continuar, el `conversacionId` devuelto anteriormente.
Omitir ese ID inicia una conversación nueva. El endpoint admite diez solicitudes
por minuto por usuario. Los enlaces apuntan al detalle público del catálogo.

La prueba de integración requiere `EVENTOS_TEST_CONNECTION` con una conexión
PostgreSQL del esquema del proyecto. Ejecutar `dotnet test`; crea datos sintéticos
en una transacción y los revierte al terminar. De forma predeterminada sustituye
el modelo por uno controlado; `EVENTOS_TEST_OLLAMA=1` utiliza Ollama real.

## Frontend — Vue + Quasar

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
y lo indica. Tampoco se ofrece un Google OAuth o correo ficticio. 
## Pendiente para la entrega final

Compras y pagos, correo, reportes, inicio de sesión con Google y el resto de
historias de usuario del proyecto.
