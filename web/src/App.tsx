import { useEffect, useState } from 'react'
import { getHealth } from './api/http'

// Başlangıç sayfası: yalnızca web → API bağlantısının çalıştığını gösterir.
// Gerçek ekranlar (giriş, danışanlar, planlar ...) src/pages altına eklenecek.
function App() {
  const [apiDurumu, setApiDurumu] = useState('kontrol ediliyor...')

  useEffect(() => {
    getHealth()
      .then(setApiDurumu)
      .catch(() => setApiDurumu('API\'ye ulaşılamadı'))
  }, [])

  return (
    <main>
      <h1>Diyetisyen Danışan Takip Sistemi</h1>
      <p>Diyetisyen ve yönetici paneli</p>
      <p>
        API durumu: <strong>{apiDurumu}</strong>
      </p>
    </main>
  )
}

export default App
