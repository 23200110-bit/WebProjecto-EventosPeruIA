import { session, signOut } from './session'
export async function api(path, options={}) {
  const controller=new AbortController()
  const timer=setTimeout(()=>controller.abort(),15000)
  try {
    const response=await fetch('/api'+path,{...options,signal:controller.signal,headers:{'Content-Type':'application/json',...(session.token?{Authorization:'Bearer '+session.token}:{}),...options.headers}})
    const data=response.status===204?null:await response.json().catch(()=>null)
    if(!response.ok){
      if(response.status===401 && session.token) signOut()
      const validation=data?.errors?Object.values(data.errors).flat().join(' '):''
      throw new Error(data?.message || validation || (response.status===401?'Inicia sesión para continuar.':response.status===404?'No se encontró el recurso.':'No se pudo completar la operación.'))
    }
    return data
  } catch(error){if(error.name==='AbortError') throw new Error('La API tardó demasiado. Intenta nuevamente.');throw error}
  finally{clearTimeout(timer)}
}
export function post(path,body){return api(path,{method:'POST',body:JSON.stringify(body)})}
export function put(path,body){return api(path,{method:'PUT',body:JSON.stringify(body)})}
export function money(value,currency='PEN'){return new Intl.NumberFormat('es-PE',{style:'currency',currency}).format(value||0)}
export function date(value){return value?new Intl.DateTimeFormat('es-PE',{dateStyle:'medium',timeStyle:'short',timeZone:'America/Lima'}).format(new Date(value)):'Por confirmar'}
