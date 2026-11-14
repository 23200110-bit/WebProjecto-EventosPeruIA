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
- `PasswordReset__PublicResetUrl`: URL HTTPS de la página de recuperación, sin query ni fragmento. Se incluye una página independiente servida por el backend en `/recuperar-contrasena`; no modifica el frontend de otro compañero.
- `PasswordReset__Smtp__Host`, `PasswordReset__Smtp__Port` (587 por defecto).
- `PasswordReset__Smtp__From`, `PasswordReset__Smtp__Username`, `PasswordReset__Smtp__Password`.
SMTP requiere STARTTLS y certificado válido; timeout de entrega15s. En Development se permite también HTTP **sólo loopback**; fuera de ese entorno se exige HTTPS. No hay configuración de proveedor/cuenta por defecto ni uso de claves de otros proyectos. Como protección básica:5 intentos por IP/15min (solicitud y consumo combinados),1correo por cuenta/minuto. Tras un proxy compartido, la IP debe resolverse en infraestructura confiable; no se confía en X-Forwarded-For enviado por cualquiera. No promete ocultar diferencias temporales del SMTP.

## Pruebas
`dotnet test` ejecuta las pruebas locales. Las nuevas pruebas HTTP/SQL se habilitan exclusivamente con `EVENTOS_FEATURE_TEST_CONNECTION` cuya base sea `eventos_feature_test`; **nunca apuntar a la base del equipo**. Crean esquema de laboratorio (citext/unaccent y tablas del modelo), limpian datos sintéticos y prueban concurrencia real. La fixture no replica triggers externos no versionados; antes de desplegar se debe verificar compatibilidad con el esquema real del equipo. Correo probado con sustituto capturador; falta acreditar entrega en el SMTP real elegido. No consume Gemini ni dinero.


## Arranque sin SMTP para el curso

Con `ASPNETCORE_ENVIRONMENT=Development`, el perfil HTTP ya existente (`dotnet run --project EventosPeruIA.API --launch-profile http`) sirve la página en:

`http://localhost:5279/recuperar-contrasena`

Ésta es la dirección del equipo donde corre la API, no una URL de un servidor que hayamos desplegado. Necesita la configuración existente de PostgreSQL y JWT. Si cambiás puerto/host, ajustá `PasswordReset__PublicResetUrl`. Una dirección localhost sólo funciona en la misma computadora donde corre el backend.

Por defecto en Development: DeliveryMode=Local. Al solicitar recuperar una cuenta Local/Activa existente, se escribe un correo `.eml` privado:

- Linux: normalmente `~/.local/share/EventosPeruIA/password-mail/`.
- Windows: `%LOCALAPPDATA%\EventosPeruIA\password-mail\`.
- Se puede definir una ruta absoluta con `PasswordReset__PickupDirectory`.

Abrir el `.eml` con el cliente de correo y seguir el enlace; en Linux el directorio es0700 y el archivo0600. No subir esos archivos a Git ni servir el directorio por HTTP; contienen enlaces de recuperación. **Este modo no envía correos a Gmail ni a Internet.** Local queda rechazado fuera de Development. Las pruebas usan directorios efímeros propios.

## Pasar a correo real

Mantener la página y configurar `PasswordReset__DeliveryMode=Smtp`, PublicResetUrl accesible para quien recibe el mensaje y los parámetros SMTP anteriores mediante user-secrets o variables de proceso. El código nunca configura una cuenta externa por sí solo ni activa pagos.

Para Gmail, si el dueño decide usar esa cuenta: Google exige verificación en dos pasos para crear una contraseña de aplicación; algunas cuentas institucionales o con Protección Avanzada no ofrecen esa opción. No usar la contraseña habitual ni pegar la contraseña de aplicación en el chat/GitHub. Preferencia general de Google: OAuth; esta integración SMTP implementa autenticación con contraseña de aplicación cuando la cuenta la permite. Si no la permite, usar otro transporte/proveedor u OAuth en una tarea explícita.

Guía oficial: https://support.google.com/accounts/answer/185833?hl=es


Ejemplo de configuración de Gmail (reemplazar valores en secretos locales, nunca en Git):
```json
{
 "PasswordReset": {
  "DeliveryMode": "Smtp",
  "PublicResetUrl": "https://TU-DOMINIO/recuperar-contrasena",
  "Smtp": {
   "Host": "smtp.gmail.com", "Port": 587,
   "From": "CORREO-DEL-EQUIPO@gmail.com",
   "Username": "CORREO-DEL-EQUIPO@gmail.com",
   "Password": "CONTRASEÑA-DE-APLICACIÓN"
  }
 }
}
```
Para una prueba en la misma computadora, Development puede mantener PublicResetUrl HTTP localhost y usar SMTP. Para que el enlace lo abra otro integrante desde su computadora, hace falta una URL HTTPS accesible y configurada; localhost no sirve fuera del equipo donde corre la API.

Referencia de parámetros SMTP/TLS: https://knowledge.workspace.google.com/admin/gmail/send-email-from-a-printer-scanner-or-app?hl=es
