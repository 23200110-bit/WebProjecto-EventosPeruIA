import { createRouter, createWebHistory } from 'vue-router'
import { session } from './session'
import Explore from './pages/Explore.vue'
import Auth from './pages/Auth.vue'
import Detail from './pages/Detail.vue'
import Profile from './pages/Profile.vue'
import Recommendations from './pages/Recommendations.vue'
import Personal from './pages/Personal.vue'
const router=createRouter({history:createWebHistory(import.meta.env.BASE_URL),routes:[
 {path:'/',component:Explore},{path:'/explorar',component:Explore},
 {path:'/evento/:id',component:Detail},
 {path:'/login',component:Auth},{path:'/registro',component:Auth},
 {path:'/perfil',component:Profile,meta:{auth:true}},
 {path:'/recomendaciones',component:Recommendations,meta:{auth:true}},
 {path:'/entradas',component:Personal,meta:{auth:true}},
 {path:'/avisos',component:Personal,meta:{auth:true}},
 {path:'/:pathMatch(.*)*',redirect:'/'}]})
router.beforeEach(to=>to.meta.auth&&!session.token?{path:'/login',query:{next:to.fullPath}}:true)
export default router
