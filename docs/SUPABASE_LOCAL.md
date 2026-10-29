# Ejecutar la API con Supabase

La API usa PostgreSQL mediante Npgsql. No requiere SQL Server ni una migración de la base existente. `appsettings.json` deja `ConnectionStrings:DevConnection` vacío deliberadamente; la aplicación rechaza el arranque si falta la configuración externa.

## Configuración local

Con .NET 10, desde `EventosPeruIA.API`, guardá la cadena completa en **user-secrets** (sólo desarrollo). El proyecto ya tiene `UserSecretsId`. Sustituí los marcadores localmente, nunca en un archivo versionado:

```powershell
dotnet user-secrets set "ConnectionStrings:DevConnection" "Host=HOST_DEL_POOLER;Port=5432;Database=postgres;Username=postgres.ID_DEL_PROYECTO;Password=CONTRASENA_LOCAL;SSL Mode=VerifyFull;Root Certificate=RUTA_LOCAL_CA_SUPABASE"
dotnet user-secrets set "JWTSettings:SecretKey" "CLAVE_LOCAL_ALEATORIA_DE_AL_MENOS_32_CARACTERES"
dotnet user-secrets set "JWTSettings:Issuer" "EventosPeruIA-local"
dotnet user-secrets set "JWTSettings:Audience" "EventosPeruIA-web-local"
dotnet run --environment Development
```

Estos comandos son ejemplos con marcadores. No pegues credenciales reales en chats, capturas o scripts compartidos. User-secrets queda fuera del repositorio, pero no cifra los valores en disco: protege tu cuenta y el equipo. La CA pública se descarga desde la configuración de conexión de Supabase; `VerifyFull` verifica tanto la cadena del certificado como el nombre del servidor.

En despliegue, el gestor de secretos debe inyectar estas variables de entorno:

```text
ConnectionStrings__DevConnection
JWTSettings__SecretKey
JWTSettings__Issuer
JWTSettings__Audience
```

El doble guion bajo corresponde a `:` en la configuración .NET. No guardes los valores en `appsettings.json`, `.env` versionados ni argumentos de procesos compartidos. No uses `dotnet user-secrets list` en una captura o log público.

## Frontend

Desde `EventosPeruIA.Web`, con las dependencias instaladas:

```powershell
$env:API_PROXY_TARGET = "http://127.0.0.1:5080"
npm run dev
```

Usá el puerto real anunciado por la API. El frontend sólo conoce `/api`: no recibe la contraseña PostgreSQL ni claves de servicio de Supabase.

## Alcance de la verificación del 29-oct-2026

Se ejecutaron consultas sobre la base Supabase existente sin migraciones ni datos de prueba insertados. La prueba añade una guarda de sólo lectura **exclusiva del laboratorio**, fuera del proyecto; la aplicación normal conserva sus operaciones autorizadas de registro y perfil. El catálogo real y el rechazo del login inexistente no prueban un login válido ni todas las operaciones de escritura en producción.

La contraseña antigua fue retirada del archivo de trabajo; esto no borra commits anteriores ni sustituye la rotación de una credencial previamente publicada. No se reescribió la historia de Git ni se publicó esta entrega.

Referencias: [configuración de secretos .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), [conexiones Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres), [TLS de Supabase](https://supabase.com/docs/guides/platform/ssl-enforcement).
