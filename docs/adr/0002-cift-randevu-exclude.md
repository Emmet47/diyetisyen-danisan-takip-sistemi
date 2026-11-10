# ADR-0002: Çift randevuyu veritabanı kısıtıyla (EXCLUDE) önlemek

**Durum:** Kabul edildi · 10.11.2026 · **İlgili:** FR-I4, BR-08

**Bağlam:** Hocamız çift randevu senaryosunun test edilmesini istiyor. Yalnızca kodda “bu saat dolu mu?” diye bakarsak iki istek aynı anda geldiğinde ikisi de saati boş görüp kaydolabilir (yarış durumu, race condition).

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| Yalnız uygulama kontrolü | Çok basit | Eşzamanlı isteklerde çift kayıt olabilir |
| Uygulama kontrolü + satır kilidi ya da SERIALIZABLE işlem | Doğru çalışır | Kilit ve yeniden deneme mantığı karmaşık; bir yerde unutulursa açık kalır |
| **PostgreSQL EXCLUDE kısıtı ✓** | Garantiyi veritabanı verir; kod basit kalır; eşzamanlı isteklerde de doğru | PostgreSQL'e özgü; EF Core modelinde tanımlanamadığı için migration'a elle SQL eklenir |

**Karar etmenleri:** Veri bütünlüğü, basitlik, test edilebilirlik.

**Karar:** Randevu çakışmasını PostgreSQL'in EXCLUDE kısıtıyla önleyeceğiz. Uygulama boş slotları hesaplayıp göstermeye devam edecek ama son sözü veritabanı söyleyecek; 23P01 hatasını “Bu saat az önce doldu” mesajlı 409 yanıtına çevireceğiz.

**Sonuçlar:**

- ➕ Aynı anda gelen iki talepten yalnızca biri kaydolur (DB-32 testiyle gösterdik).
- ➕ Uygulama kodu basit kalır.
- ➖ Başka bir veritabanına geçersek bu kuralı yeniden yazmamız gerekir.
- ➖ `btree_gist` eklentisinin açık olması gerekir.
