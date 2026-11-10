# ADR-0001: Hexagonal mimariyle tek uygulama (monolit) backend

**Durum:** Kabul edildi · 10.11.2026 · **Karar verenler:** Grup 19 · **İlgili:** NFR-B1, NFR-T1

**Bağlam:** Hocamız mimari odak olarak Hexagonal mimariyi istiyor; iş kuralları veritabanından, istemcilerden, bildirim sağlayıcısından ve dış besin API'sinden bağımsız olmalı. Dört kişilik ekibiz ve 2 Aralık'ta çalışan bir demo göstermeliyiz. Ekipte dağıtık sistem işletme deneyimi yok. Dış API ve bildirim servisi ileride değişebilir; testlerin veritabanı olmadan da çalışabilmesi gerekiyor.

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| Klasik katmanlı (Controller → Service → EF Core) | Basit, herkes biliyor | İş kuralları EF Core'a ve HTTP'ye karışır; dış servisi değiştirmek zor; test için veritabanı gerekir |
| **Hexagonal, tek uygulama ✓** | Çekirdek bağımsız; adapter değiştirmek kolay; sahte adapter'la hızlı test | Arayüz ve proje sayısı artıyor; başta öğrenmesi zaman alıyor |
| Mikroservisler | Servisler bağımsız ölçeklenir | 4 kişi ve birkaç hafta için çok ağır: ağ hataları, dağıtık işlem, birden çok veritabanı |

**Karar etmenleri:** Değiştirilebilirlik, test edilebilirlik, geliştirme basitliği, işletme kolaylığı. Dersteki ağırlıklı karar matrisiyle puanladık (1–5 puan × ağırlık):

| Ölçüt | Ağırlık | Katmanlı | Hexagonal | Mikroservis |
| --- | --- | --- | --- | --- |
| Değiştirilebilirlik | 3 | 2 | 5 | 4 |
| Test edilebilirlik | 3 | 2 | 5 | 4 |
| Geliştirme basitliği (4 kişi) | 2 | 5 | 3 | 1 |
| İşletme kolaylığı | 2 | 5 | 5 | 1 |
| **Ağırlıklı toplam (/50)** |  | **32** | **46** | **28** |

**Karar:** Backend'i Hexagonal mimariyle tek bir ASP.NET Core uygulaması olarak geliştireceğiz. İş kuralları Domain ve Application projelerinde kalacak, dış teknolojiler Infrastructure'daki adapter'larla bağlanacak ve bağımlılık kuralını mimari testle koruyacağız.

**Sonuçlar:**

- ➕ USDA, FCM ya da SeaweedFS değişirse yalnızca ilgili adapter değişir.
- ➕ Birim testleri veritabanı olmadan ve hızlı çalışır.
- ➖ Daha fazla arayüz ve dosya var; ekibin bu yapıyı öğrenmesi gerekiyor.
- ➖ Tek uygulama olduğu için bir bölümdeki aşırı yük tüm uygulamayı etkileyebilir (bizim ölçeğimizde sorun değil).

*Gözden geçirme tetikleyicisi:* p95 yanıt süresi sürekli 500 ms'yi aşarsa ya da tek sunucu yetmezse modüllerin ayrılması yeni bir ADR ile değerlendirilir.
