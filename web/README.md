# DDTS web paneli

## Yerelde açma

Node.js kurulu olmalı. Terminalde, `web` klasöründe çalıştır:

```powershell
npm.cmd ci
npm.cmd run dev -- --strictPort
```

Tarayıcıda `http://localhost:5173` adresini aç. **Diyetisyen demosu** ve **Yönetici demosu** düğmeleri, API kapalıyken örnek verilerle çalışır. Demo değişiklikleri tarayıcı sekmesinin oturumu boyunca tutulur.

## API ile çalışma

Panel, `/api` isteklerini Vite üzerinden `http://localhost:8080` adresindeki .NET API’ye iletir. API’yi başlatmak için backend kodunu ve `docker-compose.yml` dosyasını içeren DDTS proje kopyasını kullan. Kurulum adımları [GitHub’daki proje rehberinde](https://github.com/Emmet47/diyetisyen-danisan-takip-sistemi/tree/dev#readme) yer alıyor.

- API sağlık kontrolü `http://localhost:8080/api/health` adresinde, Scalar ise `http://localhost:8080/scalar` adresinde.
- Yerel API’de e-posta davetini ve parola sıfırlamayı denemek için `Web:BaseUrl` ayarını panelin adresine eşitle. Vite ile `http://localhost:5173`, Docker web paneli ile `http://localhost:3000` kullanılır. Geliştirme e-postalarını `http://localhost:8025` adresindeki Mailpit’ten aç.
- Giriş ve hesap açma, danışan yönetimi, abonelik ve yönetici işlemleri mevcut API uç noktalarını kullanır. Danışan kısıtlarında mevcut besinler kaldırılabilir. Sunucudaki besin kataloğu hazır olduğunda besin seçerek canlı kısıt eklenebilir.
- Ölçümler ve gelişim sayfasında diyetisyen kendi danışanlarının ölçümlerini ekleyebilir, düzenleyebilir ve silebilir. Danışan aynı kayıtları mobil uygulamanın **Gelişim** ekranında görür. Panel, seçilen dönemin ölçüm tablosunu tarayıcının yazdırma menüsünden PDF olarak kaydedebilir.
- Besin kataloğu ve USDA örneği, planlar ve şablonlar, öğün günlüğü, enerji hesabı, randevular ve bildirimler diyetisyen demosunda çalışır.

```powershell
npm.cmd run lint
npm.cmd run build
```
