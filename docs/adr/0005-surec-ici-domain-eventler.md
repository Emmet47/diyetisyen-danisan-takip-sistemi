# ADR-0005: Bildirimler için süreç içi domain event'ler (mesaj kuyruğu yok)

**Durum:** Kabul edildi · 10.11.2026 · **İlgili:** FR-J1, FR-J3

**Bağlam:** Plan yayımlama ve randevu olaylarında bildirim üretilmeli (Observer). Bildirim servisi (FCM) yavaşlarsa ya da çökerse asıl işlem (yayımlama, onay) başarısız olmamalı.

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| Servisin içinden doğrudan FCM çağırmak | En basit | PlanService bildirime bağımlı olur (SRP ihlali); FCM hatası işlemi bozar |
| **Süreç içi domain event + handler ✓** | Basit; commit'ten sonra çalışır; yeni tepki = yeni handler | Uygulama tam o anda çökerse push gitmeyebilir |
| Mesaj kuyruğu (RabbitMQ) | Dayanıklı; ayrı işçiler | Yeni bileşen ve izleme gerekir; bizim ölçeğimiz için fazla |

**Karar etmenleri:** Bağımsızlık (SRP), basitlik, hata yalıtımı.

**Karar:** Olayları varlıklarda üreteceğiz ve kayıt başarılı olduktan sonra aynı süreç içinde handler'lara dağıtacağız. Bildirim önce notifications tablosuna yazılacak, sonra push denenecek; push hatası asıl işlemi geri almayacak.

**Sonuçlar:**

- ➕ Asıl işlem bildirime bağlı değil; uygulama içi bildirim listesi her zaman tutarlı.
- ➖ Push gönderilemezse otomatik tekrar denenmez (bildirim uygulama içinde yine görünür).

*Gözden geçirme tetikleyicisi:* Push hataları sıklaşırsa ya da e-posta/SMS gibi yeni kanallar eklenirse kuyruk yeni bir ADR ile değerlendirilir.
