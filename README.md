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

RAG:

- Aplica `supabase/migrations/01_vector_rag.sql` a la base PostgreSQL de Supabase antes de iniciar la API.
- La cola de embeddings se procesa en segundo plano desde `document_sections` con `gemini-embedding-001` y 768 dimensiones.
- El chat RAG público está disponible en `POST /api/chat`; el chat existente autenticado permanece en `POST /api/ai/chat`.
- Este backend es .NET y conserva el cliente HTTP REST de Gemini existente; el paquete JavaScript `@google/genai` no aplica a este proyecto.
- Configura `AI:Gemini:RagChatModel` (predeterminado `gemini-4-hydrogen`) y `AI:Gemini:AdminModel` (predeterminado `gemini-4-neon`) si el proyecto tiene acceso a esos modelos.
- El agente restringido a administradores está en `POST /api/admin/agent`. Mantiene la autenticación JWT y el rol `Admin` existentes; no usa Supabase Auth.
- Para propuestas automáticas como PR draft, configura `GITHUB_TOKEN` como variable de entorno y `AdminAgent:GitHub:FrontendRepository` (`owner/repo`). El backend usa el repositorio actual por defecto. Un archivo `.env` no se carga automáticamente en ASP.NET Core. El token requiere permiso de contenido y pull requests; las propuestas nunca se fusionan automáticamente.

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
