<script setup>
import { computed, nextTick, onMounted, ref } from 'vue'
import { date, money, post } from '../api'
import { resetChat, session } from '../session'

const chat = computed(() => session.chat)
const latest = ref(null)
const suggestions = [
  { icon: 'location_on', text: '¿Qué eventos hay en Lima?' },
  { icon: 'music_note', text: '¿Qué conciertos están disponibles?' },
  { icon: 'local_activity', text: '¿Hay eventos gratuitos en Lima?' }
]

async function scrollToLatest() {
  await nextTick()
  const messages = latest.value?.parentElement
  if (messages) messages.scrollTop = messages.scrollHeight
}

onMounted(scrollToLatest)

async function send(value = chat.value.draft) {
  const state = chat.value
  const mensaje = value.trim()
  if (state.pending || !mensaje || mensaje.length > 2000) return

  state.error = ''
  state.draft = ''
  state.turns.push({ role: 'user', text: mensaje })
  state.pending = true
  void scrollToLatest()

  try {
    const result = await post('/ai/chat', {
      mensaje,
      ...(state.id ? { conversacionId: state.id } : {})
    }, { timeoutMs: 180000 })
    if (session.chat !== state) return
    state.id = result.conversacionId
    state.turns.push({ role: 'assistant', text: result.respuesta, events: result.eventos })
  } catch (error) {
    if (session.chat !== state) return
    state.turns.pop()
    state.draft = mensaje
    state.error = error.message
  } finally {
    state.pending = false
    if (session.chat === state) void scrollToLatest()
  }
}
</script>

<template>
  <q-page class="container assistant-page">
    <header class="assistant-header">
      <div class="assistant-avatar"><q-icon name="auto_awesome" size="26px" /></div>
      <div>
        <div class="eyebrow">ENCUENTRA TU PRÓXIMO PLAN</div>
        <h1>Asistente IA</h1>
        <p>Eventos, cupos y presupuestos en una conversación.</p>
      </div>
      <q-btn outline color="primary" no-caps icon="add" label="Nueva conversación"
        :disable="!chat.turns.length && !chat.draft" @click="resetChat" />
    </header>

    <section class="assistant-panel" aria-label="Chat de eventos">
      <div class="assistant-messages" role="log" aria-label="Mensajes" aria-live="polite">
        <div class="assistant-turn assistant-turn--bot">
          <div class="assistant-bubble">¡Hola! Puedo ayudarte a buscar eventos del catálogo, consultar sus cupos o calcular cuánto gastarían en entradas.</div>
        </div>
        <article v-for="(turn, index) in chat.turns" :key="index"
          :class="['assistant-turn', turn.role === 'user' ? 'assistant-turn--user' : 'assistant-turn--bot']">
          <span class="assistant-speaker">{{ turn.role === 'user' ? 'Tú' : 'Asistente IA' }}</span>
          <div class="assistant-bubble">{{ turn.text }}</div>
          <div v-if="turn.events?.length" class="assistant-events">
            <router-link v-for="event in turn.events" :key="event.eventoId"
              :to="'/evento/' + event.eventoId" class="assistant-event">
              <q-icon name="confirmation_number" size="22px" color="primary" />
              <div>
                <small>{{ event.categoria }} · {{ date(event.fechaInicio) }}</small>
                <strong>{{ event.nombre }}</strong>
                <span>{{ event.precio === 0 ? 'Gratis' : money(event.precio, event.moneda) }} · {{ event.cuposDisponibles }} cupos</span>
              </div>
              <q-icon name="chevron_right" size="20px" color="primary" />
            </router-link>
          </div>
        </article>
        <div v-if="chat.pending" class="assistant-loading" role="status">
          <q-spinner color="primary" /> Consultando el catálogo…
        </div>
        <div ref="latest" />
      </div>
      <div class="assistant-compose">
        <div v-if="!chat.turns.length" class="assistant-suggestions">
          <q-btn v-for="item in suggestions" :key="item.text" outline color="primary" no-caps
            :icon="item.icon" :label="item.text" :disable="chat.pending" @click="send(item.text)" />
        </div>
        <q-banner v-if="chat.error" class="error" role="alert">{{ chat.error }}</q-banner>
        <q-form class="assistant-form" @submit="send()">
          <q-input v-model="chat.draft" outlined label="Pregunta sobre un evento" maxlength="2000"
            :disable="chat.pending" />
          <q-btn type="submit" unelevated color="primary" no-caps icon="send" label="Enviar"
            :loading="chat.pending" :disable="!chat.draft.trim()" />
        </q-form>
      </div>
    </section>
  </q-page>
</template>
