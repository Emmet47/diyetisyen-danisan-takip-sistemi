# ADR-0003: Fotoğraflar için S3 uyumlu nesne deposu (SeaweedFS) ve süreli bağlantı

**Durum:** Kabul edildi · 10.11.2026 · **İlgili:** FR-G2, NFR-G3

**Bağlam:** Hocamız fotoğraf yüklemede dosya türü/boyutu denetimi, güvenli nesne depolama ve özel erişim bağlantıları istiyor. Öğün fotoğrafları kişisel veri; herkese açık olmamalı.

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| Veritabanında (bytea) saklamak | Her şey tek yerde | Veritabanı şişer ve yavaşlar; özel bağlantıyı yine kendimiz yazarız |
| Sunucu diskinde klasör | Çok basit | Konteyner silinince kaybolabilir; erişim kontrolünü kendimiz yazarız |
| **S3 uyumlu depo (SeaweedFS) + imzalı bağlantı ✓** | Kapalı bucket ve süreli imzalı bağlantı hazır özellik; ileride bulut S3'e geçmek yalnızca ayar değişikliği | Bir konteyner daha işletiyoruz; erişim anahtarlarını yönetmek gerekiyor |

**Karar etmenleri:** Güvenlik (gizlilik), basitlik, taşınabilirlik.

**Karar:** Fotoğrafları herkese kapalı bir SeaweedFS bucket'ında rastgele anahtarlarla saklayacağız ve görüntülemek için 10 dakika geçerli imzalı bağlantı üreteceğiz. Depoya yalnızca IPhotoStorage portu üzerinden erişeceğiz.

**Sonuçlar:**

- ➕ Bağlantı bir yere sızsa bile 10 dakika sonra işe yaramaz.
- ➕ Bulut S3'e geçmek adapter'ı değil yalnızca ayarları değiştirir.
- ➖ Danışan silinince fotoğrafları depodan ayrıca silmemiz gerekiyor (IT-13 bunu test ediyor).
