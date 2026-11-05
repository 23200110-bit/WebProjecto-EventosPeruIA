import { reactive } from 'vue'
// Memoria de la pestaña: no se guardan tokens ni contraseñas en localStorage.
const newChat = () => ({ id: null, turns: [], pending: false, draft: '', error: '' })
export const session = reactive({ token:'', user:null, chat: newChat() })
export function signIn(result) {
  if (!result?.accessToken || !result?.user) throw new Error('La API no devolvió una sesión válida.')
  session.token = result.accessToken
  session.user = result.user
  session.chat = newChat()
}
export function signOut() { session.token='';session.user=null;session.chat=newChat() }
export function resetChat() { session.chat = newChat() }
