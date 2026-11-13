# Compra y recuperación de contraseña

## Alcance
Sólo estas dos funciones. Check-in, reportes, catálogo, reseñas y demás módulos no se modifican. Registro/login conservan su contrato JSON; se añade una marca interna no serializada al JWT para invalidarlo cuando cambia la contraseña. Tokens antiguos sin marca dejan de ser válidos tras una recuperación completada. No hay cambios de tablas ni migraciones productivas automáticas.

## Compra autenticada
`POST /api/purchases`, Authorization Bearer del login:
```json
{"eventoId":123,"cantidad":2,"requestId":"29f78b49-14d1-4993-90de-310ecf827131"}
```
El cliente debe generar un UUID nuevo por compra y reutilizarlo en cada reintento de la misma compra. Usuario/precio/moneda/total vienen del servidor. Respuesta: compraId, eventoId, cantidad, precioUnitario, montoTotal, moneda, metodoPago y entradas[{entradaId,codigoCorto,qrPayload}]. QRpayload es el UUID verificable por el servidor que usa el módulo existente. El voucher se obtiene del GET ya existente `/api/Entradas/{entradaId}/voucher`.
Pago **simulado**, no se cobra dinero ni se configura una pasarela. El precio se fija al comprar. Se exige evento Publicado+Aprobado y futuro, cuenta activa, cantidad válida y cupos suficientes. Límite del evento o `Purchases__MaxTicketsPerOrder` (10 por defecto). Aforo protegido con bloqueo de fila PostgreSQL y transacción ReadCommitted; reintentos serializados con advisorylock y código único de transacción. Compra y entradas se guardan juntas o se revierten juntas. No garantiza otros escritores que inserten entradas saltándose este endpoint; éstos deben respetar el mismo bloqueo/reglas de BD.

## Recuperación
1. POST `/api/auth/forgot-password` con `{ "email": "persona@example.test" }`: 202 genérico para cuentas existentes/ausentes/bloqueadas/Google. Nunca devuelve el token. Si falta configuración global, 503. Si falla la entrega se revierte la emisión del token y se mantiene la respuesta genérica para no revelar cuentas; revisar el aviso saneado del servidor.
2. El correo contiene `PublicResetUrl#token=...`. La página de recuperación debe leer el fragmento localmente y retirarlo del historial; no enviarlo como query de navegación ni registrarlo. Después POST `/api/auth/reset-password` con `{ "token":"...", "password":"nueva...", "confirmPassword":"nueva..." }`.
3. Validez 20 minutos, 256 bits aleatorios, hash SHA256 en la BD, un solo uso. Contraseña mínimo8/máximo128, hash Identity con sal. Al usarlo se invalidan todos los enlaces pendientes y las sesiones previas; login nuevo requerido. No reactiva cuentas bloqueadas ni cambia permisos.

Configurar fuera de Git (user-secrets o variables del proceso):
- `PasswordReset__PublicResetUrl`: URL HTTPS de la pantalla del frontend, sin query ni fragmento. **Esta tarea es backend: no añade una pantalla a la rama de otro compañero.**
- `PasswordReset__Smtp__Host`, `PasswordReset__Smtp__Port` (587 por defecto).
- `PasswordReset__Smtp__From`, `PasswordReset__Smtp__Username`, `PasswordReset__Smtp__Password`.
SMTP requiere STARTTLS y certificado válido; timeout de entrega15s. No hay configuración de proveedor/cuenta por defecto ni uso de claves de otros proyectos. Como protección básica:5 intentos por IP/15min (solicitud y consumo combinados),1correo por cuenta/minuto. Tras un proxy compartido, la IP debe resolverse en infraestructura confiable; no se confía en X-Forwarded-For enviado por cualquiera. No promete ocultar diferencias temporales del SMTP.

## Pruebas
`dotnet test` ejecuta las pruebas locales. Las nuevas pruebas HTTP/SQL se habilitan exclusivamente con `EVENTOS_FEATURE_TEST_CONNECTION` cuya base sea `eventos_feature_test`; **nunca apuntar a la base del equipo**. Crean esquema de laboratorio (citext/unaccent y tablas del modelo), limpian datos sintéticos y prueban concurrencia real. La fixture no replica triggers externos no versionados; antes de desplegar se debe verificar compatibilidad con el esquema real del equipo. Correo probado con sustituto capturador; falta acreditar entrega en el SMTP real elegido. No consume Gemini ni dinero.
