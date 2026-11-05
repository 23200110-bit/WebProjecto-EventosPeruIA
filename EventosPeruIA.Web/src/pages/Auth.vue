<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { post } from '../api'
import { signIn } from '../session'

const route = useRoute()
const router = useRouter()
const register = computed(() => route.path === '/registro')
const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const busy = ref(false)
const error = ref('')
const message = ref('')
const required = value => !!value?.trim() || 'Completa este campo.'
const emailRule = value => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()) || 'Ingresa un correo válido.'
const nextQuery = computed(() => typeof route.query.next === 'string' ? { next: route.query.next } : {})

async function submit() {
  if (busy.value) return
  busy.value = true
  error.value = ''
  message.value = ''
  try {
    const credentials = { email: email.value.trim(), password: password.value }
    if (register.value) {
      await post('/auth/register', {
        ...credentials,
        firstName: firstName.value.trim(),
        lastName: lastName.value.trim(),
        confirmPassword: confirmPassword.value
      })
      password.value = ''
      confirmPassword.value = ''
      message.value = 'Tu cuenta está lista. Inicia sesión para comenzar.'
    } else {
      signIn(await post('/auth/login', credentials))
      const next = route.query.next
      await router.push(typeof next === 'string' && next.startsWith('/') && !next.startsWith('//') ? next : '/explorar')
    }
  } catch (failure) {
    error.value = failure.message
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <q-page class="auth-page">
    <aside class="auth-aside">
      <div class="auth-badge"><q-icon name="auto_awesome" /> Eventos en Perú</div>
      <h1>Tu próximo plan <em>empieza aquí.</em></h1>
      <p>Encuentra conciertos, talleres y experiencias cerca de ti.</p>
      <div class="auth-ticket" aria-hidden="true">
        <q-icon name="confirmation_number" size="48px" />
        <div><small>EVENTOS PERÚ IA</small><strong>Explora tu ciudad</strong></div>
        <q-icon name="arrow_outward" />
      </div>
    </aside>

    <section class="auth-content">
      <div class="eyebrow">TU CUENTA</div>
      <h2>{{ register ? 'Crea tu cuenta' : 'Bienvenido de vuelta' }}</h2>
      <p>{{ register ? 'Completa tus datos y encuentra tu próximo evento.' : 'Ingresa con tu correo y contraseña.' }}</p>
      <q-banner v-if="error" class="error" role="alert">{{ error }}</q-banner>
      <q-banner v-if="message" class="success" role="status">
        {{ message }}
        <router-link :to="{ path: '/login', query: nextQuery }">Iniciar sesión</router-link>
      </q-banner>

      <q-form v-else class="form-stack" @submit="submit">
        <div v-if="register" class="auth-names">
          <q-input v-model="firstName" outlined label="Nombres" maxlength="100"
            autocomplete="given-name" :rules="[required]" lazy-rules />
          <q-input v-model="lastName" outlined label="Apellidos" maxlength="100"
            autocomplete="family-name" :rules="[required]" lazy-rules />
        </div>
        <q-input v-model="email" outlined type="email" label="Correo electrónico"
          autocomplete="email" maxlength="254" :rules="[required, emailRule]" lazy-rules />
        <q-input v-model="password" outlined :type="showPassword ? 'text' : 'password'" label="Contraseña"
          :autocomplete="register ? 'new-password' : 'current-password'" maxlength="128"
          :rules="[required, value => !register || value.length >= 8 || 'Usa al menos 8 caracteres.']" lazy-rules>
          <template #append>
            <q-btn flat round dense :icon="showPassword ? 'visibility_off' : 'visibility'"
              :aria-label="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'" @click="showPassword = !showPassword" />
          </template>
        </q-input>
        <q-input v-if="register" v-model="confirmPassword" outlined type="password" label="Confirmar contraseña"
          autocomplete="new-password" maxlength="128"
          :rules="[required, value => value === password || 'Las contraseñas deben coincidir.']" lazy-rules />
        <q-btn type="submit" unelevated color="primary" no-caps
          :label="register ? 'Crear cuenta' : 'Iniciar sesión'" :loading="busy" />
      </q-form>

      <p class="auth-switch">
        {{ register ? '¿Ya tienes una cuenta?' : '¿Todavía no tienes cuenta?' }}
        <router-link :to="{ path: register ? '/login' : '/registro', query: nextQuery }">{{ register ? 'Inicia sesión' : 'Regístrate' }}</router-link>
      </p>
    </section>
  </q-page>
</template>
