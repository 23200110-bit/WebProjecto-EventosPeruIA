-- FAQs sobre compra y recepción de entradas para el chat RAG.
-- Idempotente: no duplica si ya existen. El trigger de faqs las encola para vectorizar.
insert into public.faqs (pregunta, respuesta, categoria, orden, activo)
select v.pregunta, v.respuesta, 'Entradas', v.orden, true
from (values
  ('¿Dónde y cómo compro las entradas de un evento?',
   'Entra al detalle del evento en EventosPerúIA, elige la cantidad de entradas y pulsa Comprar. Debes iniciar sesión (con correo y contraseña o con Google). La compra se hace dentro de la plataforma; el pago es simulado en esta demostración.', 900),
  ('¿Cómo recibo mis entradas después de comprar?',
   'Al terminar la compra recibes un correo con el código QR de cada entrada. También las encuentras en la sección Mis entradas. Presenta el QR en la puerta del evento.', 901),
  ('¿Puedo comprar entradas sin iniciar sesión?',
   'No. Para comprar necesitas una cuenta: regístrate con tu correo o usa Continuar con Google. Así tus entradas quedan asociadas a ti.', 902)
) as v(pregunta, respuesta, orden)
where not exists (select 1 from public.faqs f where f.pregunta = v.pregunta);
