<script>
import { session, signOut } from './session'
export default {data:()=>({session,menu:false}),methods:{logout(){signOut();this.$router.push('/login')}}}
</script>
<template>
 <q-layout view="hHh lpR fFf">
  <q-header class="topbar"><q-toolbar class="toolbar">
   <router-link to="/" class="brand"><span class="brand-mark">e.</span> eventos<span class="brand-peru">perú</span><small>IA</small></router-link>
   <q-space/><nav class="desktop-nav"><router-link to="/explorar">Explorar eventos</router-link><router-link v-if="session.user" to="/recomendaciones">Para ti</router-link><router-link v-if="session.user" to="/entradas">Mis entradas</router-link><router-link v-if="session.user" to="/avisos">Avisos</router-link></nav>
   <q-btn v-if="!session.user" flat no-caps label="Iniciar sesión" to="/login" class="desktop-nav"/>
   <q-btn v-if="!session.user" unelevated color="primary" no-caps label="Crear cuenta" to="/registro"/>
   <q-btn v-if="session.user" flat no-caps :label="session.user.firstName" to="/perfil"/>
   <q-btn v-if="session.user" flat no-caps label="Salir" @click="logout"/>
   <q-btn flat label="☰" aria-label="Abrir menú" class="mobile-menu" @click="menu=!menu"/>
  </q-toolbar><div v-if="menu" class="mobile-links"><router-link to="/explorar" @click="menu=false">Explorar</router-link><router-link to="/recomendaciones" @click="menu=false">Recomendaciones</router-link><router-link to="/perfil" @click="menu=false">Perfil</router-link><router-link to="/entradas" @click="menu=false">Mis entradas</router-link><router-link to="/avisos" @click="menu=false">Avisos</router-link><router-link to="/login" @click="menu=false">Iniciar sesión</router-link></div></q-header>
  <q-page-container><router-view :key="$route.fullPath"/></q-page-container>
  <footer class="footer"><span class="brand">eventosperú <small>IA</small></span><span>Conecta con tu ciudad. Vive algo nuevo.</span><span>Proyecto académico · Semana 12</span></footer>
 </q-layout>
</template>
