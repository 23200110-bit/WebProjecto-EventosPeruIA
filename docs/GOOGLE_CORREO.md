# Login con Google y correos reales

Rama: `feature/google-login-correos`.

## Endpoints nuevos
- `POST /api/auth/google` `{ "idToken": "..." }` → `{ accessToken, user }` (crea la cuenta si el correo no existe, `ProveedorAuth="Google"`).
- `POST /api/purchases` ahora además envía al comprador un correo con el QR de cada entrada (si el correo falla, la compra no se revierte).

## Configuración (user-secrets, NUNCA en Git)
```
cd EventosPeruIA.API
dotnet user-secrets set "Google:ClientId" "XXXX.apps.googleusercontent.com"
dotnet user-secrets set "Email:DeliveryMode" "Smtp"
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "tucorreo@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "<contraseña de aplicación de 16 letras>"
dotnet user-secrets set "Email:Smtp:From" "tucorreo@gmail.com"
```
- Sin `Email:DeliveryMode` (o `Local`, solo Development) los correos se guardan como `.html` en `%LOCALAPPDATA%\EventosPeruIA\mail\`.
- Si no defines `Email:Smtp:*`, se usan los `PasswordReset:Smtp:*` ya existentes.
- La recuperación de contraseña usa `PasswordReset:DeliveryMode=Smtp` + `PasswordReset:Smtp:*` (sin cambios).

## Google Cloud
1. console.cloud.google.com → Credenciales → ID de cliente OAuth → Aplicación web.
2. Orígenes autorizados: `http://localhost:5173` (y el dominio de producción).
3. Pantalla de consentimiento en modo "Testing": agrega los correos de prueba.
4. El Client ID es público (va también en el frontend como `VITE_GOOGLE_CLIENT_ID`); no hay client secret en este flujo.
