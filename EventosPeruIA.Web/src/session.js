import { reactive } from 'vue'
// Memoria de la pestaña: no se guardan tokens ni contraseñas en localStorage.
export const session = reactive({ token:'', user:null })
export function signIn(result) { session.token=result.accessToken; session.user=result.user }
export function signOut() { session.token='';session.user=null }
