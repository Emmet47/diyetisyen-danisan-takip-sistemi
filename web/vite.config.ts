import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Geliştirmede /api ile başlayan istekler yerelde çalışan .NET API'ye gider.
    // Tarayıcı aynı adrese istek attığını sandığı için CORS ayarına gerek kalmaz.
    // (Docker'da aynı işi web/nginx.conf yapıyor.)
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
})
