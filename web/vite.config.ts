import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  server: {
    port: 5173,
    // Geliştirmede /api ile başlayan istekler yerelde çalışan .NET API'ye gider.
    // Tarayıcı aynı adrese istek attığını sandığı için CORS ayarına gerek kalmaz.
    // (Docker'da aynı işi web/nginx.conf yapıyor.)
    // API başka adreste çalışıyorsa ortam değişkeniyle değiştirilebilir: DDTS_API_URL=http://localhost:5080 npm run dev
    proxy: {
      '/api': { target: process.env.DDTS_API_URL ?? 'http://localhost:8080', changeOrigin: true },
    },
  },
})
