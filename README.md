# EventosPeruIA

Requisitos: SDK .NET 10, PostgreSQL del proyecto en Supabase y una clave de Gemini.

Abrir `ProyectoFinalWeb-EeventosPeruIA.slnx` y configurar en secretos de usuario de `EventosPeruIA.API`:

- `ConnectionStrings:DevConnection`: cadena PostgreSQL del equipo.
- `JWTSettings:SecretKey`: clave aleatoria de al menos 32 bytes.
- `JWTSettings:Issuer`: `EventosPeruIA`.
- `JWTSettings:Audience`: `EventosPeruIA.Web`.
- `AI:Gemini:ApiKey`: clave del servidor, nunca del frontend.

No guardar credenciales en Git. En el servidor pueden configurarse como variables de entorno usando `__` en lugar de `:`.

```sh
dotnet restore
dotnet run --project EventosPeruIA.API
```

API local: http://localhost:5279. OpenAPI: `/openapi/v1.json`.
Solicitudes ejecutables: `EventosPeruIA.API/ProyectoFinalWeb.http`.
Modelos configurables: `AI:Gemini:ChatModel` y `AI:Gemini:EmbeddingModel`.
La API utiliza la cuota disponible del proyecto Gemini; no activa facturación automáticamente.

Frontend:

```sh
cd EventosPeruIA.Web
npm ci
npm run dev
```

Configurar `API_PROXY_TARGET=http://localhost:5279` antes de iniciar Vite.

Pruebas locales: `dotnet test`.
Integración con PostgreSQL: configurar `EVENTOS_TEST_CONNECTION`; los registros sintéticos se revierten al terminar.
Para las pruebas con Gemini real, configurar además `EVENTOS_TEST_GEMINI_KEY` (consume cuota).
