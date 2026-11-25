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
- `Rag:MatchThreshold` (opcional, 0-1, por defecto 0.6) controla qué tan parecida debe ser una fuente a la pregunta. Aplica también `supabase/migrations/02_faq_compra_entradas.sql` para las FAQs de compra de entradas.
- El chat RAG público está disponible en `POST /api/chat`; el chat existente autenticado permanece en `POST /api/ai/chat`.
- El chatbot debe enviar los reportes explícitos de usuarios autenticados a `POST /api/ai/bug-reports` con `{ "description": "..." }`. Supabase agrupa descripciones normalizadas iguales; solo cuenta una vez por usuario y día.
- Tres cuentas distintas que reporten el mismo problema dentro de 24 horas ponen la incidencia en cola. El worker local la procesa si el runner está activo; las incidencias, reportes y resultados quedan en PostgreSQL/Supabase.
- Este backend es .NET y conserva el cliente HTTP REST de Gemini existente; el paquete JavaScript `@google/genai` no aplica a este proyecto.
- Configura `AI:Gemini:RagChatModel` (predeterminado `gemini-4-hydrogen`) y `AI:Gemini:AdminModel` (predeterminado `gemini-4-neon`) si el proyecto tiene acceso a esos modelos.
- El agente restringido a administradores está en `POST /api/admin/agent`. Mantiene la autenticación JWT y el rol `Admin` existentes; no usa Supabase Auth.
- Para habilitar actualizaciones autónomas en local, inicia el backend mediante `scripts/run-local-agent.ps1`. El agente administrativo solo modifica `.cs` versionados en API/CORE; el procesamiento automático de reportes queda restringido a servicios permitidos y no puede tocar autenticación, pagos, controles del agente ni ejecutar acciones de base de datos. Ejecuta `dotnet test` y `dotnet build`, revierte si fallan, y el supervisor reinicia/verifica `/health`, también con rollback si no queda sano.
- El runner local debe permanecer abierto y tener las variables de entorno/secretos de desarrollo configurados. Sin el runner, la herramienta de actualización permanece deshabilitada.
- Las instrucciones de usuarios y documentos RAG se mantienen fuera del mensaje de sistema; se rechazan patrones comunes de inyección y los reportes sospechosos se excluyen del diagnóstico automático. Esto es defensa en profundidad, no una garantía contra toda variante de prompt injection. Los reportes nunca habilitan SQL/comandos arbitrarios, y cualquier parche sigue limitado por allowlist, pruebas, compilación y health check; el modo local puede aplicar cambios sin aprobación humana.

```powershell
.\scripts\run-local-agent.ps1
```

Borrador desde flyer: `POST /api/eventos/flyer-borrador` (autenticado) recibe la imagen en base64 y devuelve datos sugeridos sin guardar nada; modelo configurable con `AI:Gemini:FlyerModel` (por defecto `AI:Gemini:ChatModel`, debe admitir imágenes).

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
