import { createApp } from 'vue'
import { Quasar } from 'quasar'
import es from 'quasar/lang/es'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/dist/quasar.css'
import './style.css'
import App from './App.vue'
import router from './router'
createApp(App).use(Quasar,{lang:es,config:{brand:{primary:'#245444',secondary:'#D97142',accent:'#EBE7DC'}}}).use(router).mount('#app')
