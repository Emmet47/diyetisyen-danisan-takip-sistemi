# Web için API kullanım rehberi

Bu rehber, backend'in (.NET API) hazırladığı uç noktaları web paneline nasıl bağlayacağını adım adım anlatır.
Backend kodunu okuman gerekmez: ihtiyacın olan her şey **Scalar** sayfasında.

> **Kısa sözlük**
> - **API**: Web panelinin veri aldığı/gönderdiği sunucu programı (bizde .NET ile yazılı).
> - **Uç nokta (endpoint)**: API'deki tek bir işlem. Bir **metot** + bir **yol**dan oluşur, ör. `GET /api/clients`.
> - **Metot**: `GET` = oku, `POST` = yeni kayıt, `PUT` = güncelle, `DELETE` = sil.
> - **JSON**: API ile konuştuğumuz veri biçimi, ör. `{ "fullName": "Ayşe Yılmaz", "heightCm": 165 }`.
> - **Scalar**: API'nin tüm uç noktalarını listeleyen ve tarayıcıdan deneyebildiğin sayfa (Swagger'ın daha yeni bir benzeri).
> - **JWT (token)**: Girişte API'nin verdiği uzun metin. "Ben giriş yaptım" kanıtı; her isteğe eklenir.

## 1. Sistemi aç

1. Docker Desktop açık olsun.
2. Repo kökünde (yalnızca ilk sefer) `.env` dosyasını oluştur ve boş değerleri doldur:
   ```bash
   cp .env.example .env
   ```
   `POSTGRES_PASSWORD`, `S3_SECRET_KEY` için istediğin bir şey; `JWT_SIGNING_KEY` için en az 32 karakterlik rastgele bir metin yaz.
3. Veritabanı + API'yi aç:
   ```bash
   docker compose up -d --build postgres seaweedfs mailpit api
   ```
4. Web panelini kendi bilgisayarında çalıştır (kaydedince sayfa kendini yeniler):
   ```bash
   cd web
   npm ci        # ilk sefer ve package-lock.json değişince
   npm run dev   # http://localhost:5173
   ```

Backend'de yeni uç nokta eklendiyse: `git pull` yapıp 3. adımdaki komutu tekrar çalıştır (`--build` API'yi yeniden derler).

## 2. Scalar'da uç noktayı bul

1. Tarayıcıda aç: **http://localhost:8080/scalar**
2. Sol menüde uç noktalar gruplar hâlinde (Auth, Clients, Plans, Appointments ...) listelenir.
3. Bir uç noktaya tıklayınca sağda şunları görürsün:
   - **Metot ve yol**: ör. `POST /api/clients`
   - **Parameters**: yoldaki (`/api/clients/{id}` → `id`) veya adres sonundaki (`?page=1&pageSize=20`) değerler
   - **Body (istek gövdesi)**: göndermen gereken JSON alanları, tipleri ve hangilerinin zorunlu olduğu (`required`)
   - **Responses (yanıtlar)**: her durum kodu için dönen JSON'un şekli ve örneği

## 3. Tarayıcıdan dene

1. Uç noktada **Test Request** düğmesine bas.
2. Gövde gerekiyorsa örnek JSON'u düzenle, **Send** ile gönder.
3. Altta durum kodunu ve dönen JSON'u görürsün. Kod yazmadan önce hep böyle dene: neyin döndüğünü gözünle gör.

**Giriş gerektiren uç noktalar:**

1. Hesabın yoksa `POST /api/auth/register` ile diyetisyen hesabı aç (paket kimliğini `GET /api/subscription-plans` yanıtından al). Varsa `POST /api/auth/login` uç noktasını e-posta ve parolayla dene.
2. Yanıttaki `accessToken` değerini kopyala.
3. Denediğin uç noktada **Headers** bölümüne `Authorization` başlığını ekle, değeri `Bearer <token>` olsun (araya bir boşluk).
4. Artık o kullanıcı olarak istek atarsın. Token 60 dakika geçerli; süre dolunca 401 alırsın, yeniden giriş yap.

Parola sıfırlama e-postaları gerçek adrese gitmez, Mailpit'e düşer: http://localhost:8025 → bağlantıdaki `token=` değerini `POST /api/auth/reset-password` isteğinde kullan.

## 4. Durum kodları ne demek?

| Kod | Anlamı | Web'de ne yapmalı |
| --- | --- | --- |
| 200 / 201 / 204 | Başarılı (201: yeni kayıt oluştu, 204: gövdesiz başarı) | Sonucu göster |
| 400 | Girdi hatalı veya iş kuralı ihlali | `errors` içindeki mesajları ilgili form alanının altında göster |
| 401 | Giriş yok / token süresi doldu | Giriş sayfasına yönlendir |
| 403 | Rolün bu işlemi yapamaz | "Yetkiniz yok" mesajı |
| 404 | Kayıt yok ya da başkasına ait | "Bulunamadı" mesajı |
| 409 | Durum çakışması (paket limiti dolu, saat dolu ...) | `title` + `detail` mesajını göster (ör. "Bu saat az önce doldu. Lütfen başka bir saat seç.") |
| 429 | Çok fazla giriş denemesi | "Biraz sonra tekrar deneyin" |
| 503 | Dış kaynak (USDA) yok, ör. besin içe aktarma | "Biraz sonra tekrar dene" |

`src/api/http.ts` bu mesajları kendisi üretir: `detail` varsa onu, yoksa durum koduna göre Türkçe hazır metni kullanır. Sayfada yalnızca `errorText(hata)` (src/ui.tsx) göster.

## 5. Koda dökmek

Ortak yardımcı hazır: `src/api/http.ts`. `fetch` yazmana gerek yok; token ekleme, hata metinleri ve 401'de girişe dönme orada yapılıyor.
Bütün modüllerin dosyaları da hazır: `auth.ts`, `clients.ts`, `subscription.ts`, `admin.ts`, `measurements.ts`, `reports.ts`,
`foods.ts`, `energy.ts`, `plans.ts` (plan + şablon), `mealLogs.ts`, `appointments.ts`, `notifications.ts`. Tipler `models.ts`'te.

Yeni bir uç nokta eklenirse:

1. İlgili modül dosyasına küçük bir fonksiyon ekle (yoksa `src/api/` altında yeni dosya aç).
2. Scalar'daki **Responses** şemasına bakıp TypeScript tipini `models.ts`'e yaz (alan adları birebir aynı olmalı).
3. Uç noktayı `api.get / api.post / api.put / api.delete` ile çağır; dosya indirmek için `api.download`.

Örnek (`GET /api/subscription-plans`, kayıt ekranındaki paketler):

```ts
// Örnek (gerçek hali src/api/auth.ts içinde authApi.packages)
import { api } from './http'

// Alan adları sınıf diyagramındaki SubscriptionPlan ile aynı (docs/uml/cd-1), JSON'da camelCase.
// Yeni bir tip yazarken adları ve tipleri Scalar'daki yanıt şemasından birebir al.
export type SubscriptionPlan = {
  id: string
  name: string
  maxClients: number
  monthlyPrice: number
  isActive: boolean
}

export function getSubscriptionPlans() {
  return api.get<SubscriptionPlan[]>('/subscription-plans')   // başına /api otomatik eklenir
}
```

Sayfada kullanmak ve hatayı göstermek:

```tsx
import { ApiError } from '../api/http'
import { getSubscriptionPlans, type SubscriptionPlan } from '../api/subscriptionPlans'   // örnek dosya adı

const [paketler, setPaketler] = useState<SubscriptionPlan[]>([])
const [hata, setHata] = useState<string | null>(null)

useEffect(() => {
  getSubscriptionPlans()
    .then(setPaketler)
    .catch((e) => setHata(e instanceof ApiError ? e.message : 'Beklenmeyen bir hata oluştu'))
}, [])
```

Giriş yapınca token'ı sakla, çıkışta sil:

```ts
import { api, setToken } from './http'

// Yanıt biçimi gereksinim belgesinin 8.3 bölümündeki örnekle aynı
type LoginResponse = { accessToken: string; expiresAt: string; role: 'Admin' | 'Dietitian' | 'Client' }

const sonuc = await api.post<LoginResponse>('/auth/login', { email, password })
setToken(sonuc.accessToken)   // bundan sonraki tüm istekler bu token'la gider
// çıkış: setToken(null)
```

## 6. Kurallar

- Adresleri hep `/api/...` diye yaz, `http://localhost:8080` yazma. Geliştirmede Vite, Docker'da nginx doğru yere iletir.
- API anahtarı (USDA vb.) web koduna asla girmez; onlar yalnızca API'de durur.
- Tarih-saatler API'de UTC ve ISO 8601 biçiminde (`2026-12-01T07:00:00Z`); ekranda Türkiye saatine çevirip göster.
- Uzun listeler sayfalı: `?page=1&pageSize=20` (sayfa boyutu en fazla 100). Sayfalı yanıtın biçimi: `{ items: [...], page, pageSize, totalCount }`; toplam sayfa = `Math.ceil(totalCount / pageSize)`.
- Yönetici sayfaları (`/api/admin/...`) için giriş: yerelde user-secrets'taki `Seed:AdminEmail` / `Seed:AdminPassword`, Docker'da `.env`'deki `ADMIN_EMAIL` / `ADMIN_PASSWORD` ile `POST /api/auth/login`.
- Parola sıfırlama gibi e-postalar gerçekten gitmez; **http://localhost:8025** (Mailpit) gelen kutusunda görünür.
- Ölçüm geçmişi (`GET /api/clients/{id}/measurements`, mobilde `GET /api/me/measurements`) şu biçimde döner: `{ targetWeightKg, remainingKg, items: [...] }`. `items` eskiden yeniye sıralı (grafik bu sırayla çizilir); her satırda bir önceki ölçüme göre fark var (`weightChangeKg`, `waistChangeCm` ..., ilk satırda `null`). `targetWeightKg` grafikteki hedef çizgisi, `remainingKg` hedefe kalan kilo. Aynı gün ikinci ölçüm 409 döner: kullanıcıya o günkü ölçümü düzenlemesini söyle (`PUT .../measurements/{measurementId}`).
- **Saatler:** randevu zamanları UTC gider/gelir (sonu `Z`): `2026-12-01T07:00:00Z` = 1 Aralık 10:00 Türkiye saati. Ekranda göstermek için 3 saat ekle (`new Date(...)` tarayıcıda bunu kendisi yapar). Öğün saati ve çalışma saati ise Türkiye saatidir ve `"08:00:00"` biçiminde yazılır.
- **Plan / şablon gövdesi:** `meals: [{ dayNo: 1, name: "Kahvaltı", time: "08:00:00", items: [{ foodItemId, portionCount: 2, alternatives: [{ foodItemId, portionCount: 1 }] }] }]`. Günlük planda `dayNo` hep 1; haftalık planda 1 = Pazartesi … 7 = Pazar. Hatalı plan 400 döner ve `errors.genel` listesinde bütün hatalar birlikte gelir; sevmediği besin gibi uyarılar 201 yanıtının `warnings` listesindedir.
- **Besin arama:** `GET /api/foods?q=yulaf&external=true`. `externalUnavailable: true` gelirse `message`'ı sarı uyarı olarak göster; liste yine yerel sonuçlarla dolu gelir. Dış kaynaktan gelen satırda `id` boş, `externalId` dolu: plana eklemeden önce `POST /api/foods/import` ile aktar (Türkçe ad verebilirsin), dönen `id`'yi kullan.
- **Öğün fotoğrafı:** `POST /api/me/meal-logs/{id}/photo`, `multipart/form-data`, alan adı `file`. Yalnızca JPEG/PNG, en fazla 5 MB. Görüntülemek için `GET /api/meal-logs/{id}/photo-url` → `{ url, expiresAt }` (10 dakika geçerli; süresi dolunca yeniden iste).
- **Bildirimler:** `GET /api/notifications?page=1&pageSize=20` (sayfalı, en yenisi önce), okununca `POST /api/notifications/{id}/read`.
- **Demo verisi:** panelde koda gömülü örnek veri yok (NFR-D1). Denemek için hesabı "Hesap oluştur" ile aç, danışan, besin ve planları sahte bilgilerle (`@ornek.test`, "Deneme Danışan") arayüzden gir.
- **Besin düzenleme:** ortak (hazır) besinler düzenlenemez; denenirse API 403 döner ve "Ortak besin kayıtları düzenlenemez..." mesajı gösterilir.
- **Diyetisyenin boş saatleri:** randevu oluştururken boş saatler `GET /api/working-hours` ve o günün `GET /api/appointments` listesinden hesaplanır (30 dakikalık slotlar). Son kontrolü API yapar; çakışırsa 409.
- Danışan eklenince (`POST /api/clients`) danışana giden davet e-postası da Mailpit'e düşer. Davet bağlantısı parola sıfırlamayla aynı `/reset-password?token=...` sayfasını açar; danışan orada parolasını belirler, sonra mobil uygulamadan giriş yapar.

## 7. Takıldığında

- Uç nokta Scalar'da yoksa ya da beklediğinden farklıysa: GitHub'da issue aç (başlık: hangi uç nokta, ne bekliyordun, ne geldi).
- API açılmıyorsa: `docker compose logs api` çıktısını issue'ya ekle.
- Ham OpenAPI belgesi (tüm uç noktaların makine okunur listesi): http://localhost:8080/openapi/v1.json
