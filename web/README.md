# web — Diyetisyen ve yönetici paneli (React + Vite + TypeScript)

## Çalıştırma (geliştirme)

1. API'nin çalıştığından emin olun (`http://localhost:8080`) — adımlar [API_REHBERI.md](API_REHBERI.md) içinde.
2. Bağımlılıkları kurun (ilk sefer ve `package-lock.json` değişince):
   ```bash
   cd web
   npm ci
   ```
3. Geliştirme sunucusunu açın:
   ```bash
   npm run dev
   ```
   Adres: http://localhost:5173 — `/api` istekleri Vite proxy'si ile API'ye gider (`vite.config.ts`).

## Komutlar

| Komut | Ne yapar |
| --- | --- |
| `npm run dev` | Geliştirme sunucusu (kaydedince sayfa kendini yeniler) |
| `npm run lint` | oxlint ile kod denetimi |
| `npm run build` | TypeScript kontrolü + üretim derlemesi (`dist/`) |

## Klasör düzeni (önerilen)

```
src/
├── api/          # API çağrıları (http.ts ortak yardımcı, modül başına bir dosya: clients.ts, plans.ts ...)
├── components/   # tekrar kullanılan parçalar (tablo, form alanı, grafik ...)
├── pages/        # sayfalar, modül başına klasör (clients/, plans/, appointments/, admin/ ...)
├── App.tsx
└── main.tsx
```

Paket eklemek için `npm install <paket>` kullanın; `package-lock.json` dosyasını da commit'leyin.
