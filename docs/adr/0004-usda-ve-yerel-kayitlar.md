# ADR-0004: Dış besin verisi için USDA FoodData Central ve yerel kayıtlara dönüş

**Durum:** Kabul edildi · 10.11.2026 · **İlgili:** FR-D3, FR-D4, NFR-E1, NFR-G5

**Bağlam:** Hocamız dış besin verisi sağlayıcısı entegrasyonunda kota, hata yönetimi, veri kaynağı ve güncellik bilgisi istiyor; API anahtarları yalnızca sunucuda kalmalı. Dış API erişilemezse sistem güvenli biçimde davranmalı ve daha önce doğrulanmış yerel kayıtlar kullanılabilmeli.

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| İstemciler dış API'yi doğrudan çağırır | Kolay | API anahtarı istemciye sızar; hata yönetimi her istemcide ayrı yazılır |
| Her aramada sunucu dış API'ye gider, hiçbir şey saklanmaz | Veri hep güncel | Kota çabuk dolar; API çökünce arama da çöker |
| **Sunucuda adapter + seçilen besini yerelde saklamak ✓** | Anahtar sunucuda; besin kaynak ve tarihiyle saklanır; API çökse de yerel kayıtlarla devam edilir | Yerel kayıt zamanla eskiyebilir (bu yüzden tarihini gösteriyoruz) |

**Karar etmenleri:** Hata toleransı, gizli anahtar güvenliği, kota.

**Karar:** Dış besin verisine yalnızca sunucudaki UsdaFoodDataCentralAdapter üzerinden erişeceğiz. 3 saniyelik zaman aşımı ve saatte 900 isteklik kota uygulayacağız (USDA'nın sınırı IP başına saatte 1.000). Diyetisyenin seçtiği besini kaynak adı ve çekilme tarihiyle yerel veritabanına aktaracağız; dış kaynak kullanılamazsa yerel kayıtlarla devam edeceğiz.

**Sonuçlar:**

- ➕ API anahtarı hiçbir istemciye gitmez.
- ➕ USDA çökse de plan hazırlanabilir.
- ➖ USDA veritabanı İngilizce; dış aramada İngilizce terim kullanmak gerekiyor, içe aktarırken Türkçe ad düzeltilebilir.
- ➖ Kota sayacı bellekte tutulduğu için uygulama yeniden başlayınca sıfırlanıyor (tek sunucu için kabul edilebilir).
