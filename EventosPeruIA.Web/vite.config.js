import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
export default defineConfig({plugins:[vue({template:{transformAssetUrls}}),quasar()],server:{proxy:{'/api':{target:process.env.API_PROXY_TARGET || 'http://127.0.0.1:5080',changeOrigin:true}}}})
