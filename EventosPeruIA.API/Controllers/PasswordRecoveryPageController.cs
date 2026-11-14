using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
namespace EventosPeruIA.API.Controllers;
[ApiController]
public sealed class PasswordRecoveryPageController:ControllerBase {
 [HttpGet("/recuperar-contrasena")]
 public IActionResult Page(){
  var nonce=Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
  Response.Headers.CacheControl="no-store";
  Response.Headers["Referrer-Policy"]="no-referrer";
  Response.Headers["X-Content-Type-Options"]="nosniff";
  Response.Headers["X-Frame-Options"]="DENY";
  Response.Headers["Content-Security-Policy"]=$"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'nonce-{nonce}'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
  return Content(Html.Replace("__NONCE__",nonce),"text/html; charset=utf-8");
 }
 const string Html="""
<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Recuperar contraseña — EventosPeruIA</title>
<style nonce="__NONCE__">*{box-sizing:border-box}body{font:16px system-ui,sans-serif;background:#f3f5fa;color:#17243b;margin:0;padding:32px 16px}main{max-width:480px;margin:5vh auto;background:white;padding:28px;border-radius:16px;box-shadow:0 8px 35px #17243b14}h1{font-size:26px;line-height:1.2}p{line-height:1.55;color:#42516b}label{display:block;margin-top:18px;font-weight:600}input,button{font:inherit;width:100%;padding:12px;border:1px solid #a7b1c3;border-radius:8px;margin-top:8px}button{background:#2348b5;color:white;border:0;margin-top:24px;cursor:pointer}button:disabled{opacity:.6;cursor:wait}input:focus,button:focus{outline:3px solid #a6bcff;outline-offset:2px}#status{min-height:24px;white-space:pre-wrap}.error{color:#a20d22!important}[hidden]{display:none!important}</style></head>
<body><main><h1>Recuperar contraseña</h1><p id="intro">Te enviaremos un enlace para elegir una contraseña nueva.</p>
<form id="request-form"><label for="email">Correo de tu cuenta</label><input id="email" type="email" autocomplete="email" maxlength="254" required><button type="submit">Solicitar enlace</button></form>
<form id="reset-form" hidden><label for="password">Nueva contraseña</label><input id="password" type="password" autocomplete="new-password" minlength="8" maxlength="128" required><label for="confirm">Repetir contraseña</label><input id="confirm" type="password" autocomplete="new-password" minlength="8" maxlength="128" required><button type="submit">Guardar contraseña</button></form>
<p id="status" role="status" aria-live="polite"></p><noscript>Necesitás activar JavaScript para enviar este formulario.</noscript></main>
<script nonce="__NONCE__">
(()=>{'use strict';
 const status=document.getElementById('status'),requestForm=document.getElementById('request-form'),resetForm=document.getElementById('reset-form');
 let token='';
 const pathname=location.pathname.replace(/\/+$/,''),suffix='/recuperar-contrasena';
 const prefix=pathname.endsWith(suffix)?pathname.slice(0,-suffix.length):'';
 function message(text,error=false){status.textContent=text;status.classList.toggle('error',error);}
 function readLink(){
  const incoming=new URLSearchParams(location.hash.slice(1)).get('token')||'';
  history.replaceState(null,'',location.pathname);
  if(incoming){token=incoming;requestForm.hidden=true;resetForm.hidden=false;message('');document.getElementById('intro').textContent='Elegí una contraseña de entre 8 y 128 caracteres.';}
 }
 readLink();window.addEventListener('hashchange',readLink);
 async function send(form,endpoint,payload){
  const button=form.querySelector('button');button.disabled=true;message('Procesando…');
  try{
   const response=await fetch(prefix+'/api/auth/'+endpoint,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload),credentials:'omit',referrerPolicy:'no-referrer'});
   let data={};try{data=await response.json();}catch{}
   if(!response.ok){message(response.status===429?'Esperá unos minutos antes de volver a intentarlo.':(data.mensaje||data.message||'No pudimos procesar la solicitud. Revisá los datos o solicitá un enlace nuevo.'),true);return;}
   message(data.mensaje||'Solicitud completada.');
   if(endpoint==='reset-password'){token='';resetForm.reset();resetForm.hidden=true;document.getElementById('intro').textContent='Ya podés volver a la aplicación e iniciar sesión con tu nueva contraseña.';}
  }catch{message('No pudimos conectar con el servidor. Intentá nuevamente.',true);}
  finally{button.disabled=false;}
 }
 requestForm.addEventListener('submit',event=>{event.preventDefault();send(requestForm,'forgot-password',{email:document.getElementById('email').value.trim()});});
 resetForm.addEventListener('submit',event=>{event.preventDefault();const password=document.getElementById('password').value;
  if(password!==document.getElementById('confirm').value){message('Las contraseñas no coinciden.',true);return;}
  if(!token){message('Solicitá un enlace nuevo.',true);return;}
  send(resetForm,'reset-password',{token,password,confirmPassword:password});
 });
})();
</script></body></html>
""";
}
