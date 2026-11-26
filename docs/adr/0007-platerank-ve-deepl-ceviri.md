# ADR-0007: Türk yemekleri için PlateRank, Türkçe arama için DeepL çevirisi

**Durum:** Kabul edildi · 22.11.2026 · ADR-0004'ün Türkçe arama kısmının yerine geçer · **İlgili:** FR-D1, FR-D3, FR-D4, NFR-B1, NFR-G5

**Bağlam:** USDA FoodData Central'da mercimek çorbası, ezogelin, kısır, mantı gibi Türk yemekleri yok; diyetisyenler en çok bunları arıyor. Ayrıca USDA İngilizce. İlk sürümde adapter içindeki küçük bir TR→EN sözlükle aramayı çeviriyorduk ama sözlükte olmayan kelime aranamıyordu ve gelen adlar İngilizce kalıyordu.

**Değerlendirilen seçenekler (Türk yemekleri):**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **PlateRank (platerank.dev) ✓** | 185 Türk yemeği, adlar Türkçe; salt-okunur açık REST ucu; kendi verisi CC BY 4.0 (atıfla kullanılır) | Değerlerin çoğu ölçüm değil tahmin; Türk yemeklerinde porsiyonun gramı yok, değerler porsiyon başına |
| TürKomp (Tarım ve Orman Bakanlığı) | Resmi ve ölçülmüş veri | Açık API'si yok, veri seti satılıyor |
| Diyetisyen Türk yemeklerini elle girer | Ek bağımlılık yok | Her diyetisyen aynı yemekleri tekrar tekrar girer |

**Değerlendirilen seçenekler (çeviri):**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| **DeepL API Free ✓** | Türkçe çeviri kalitesi iyi; ayda 500.000 karakter ücretsiz, kart istemiyor; birden çok metni tek istekte çeviriyor | Hesap açıp anahtar almak gerekiyor |
| Google Cloud Translation | Kaliteli, ayda 500.000 karakter ücretsiz | Faturalandırma hesabı (kart) istiyor |
| MyMemory | Anahtar gerekmiyor | Günde 5.000 karakter, kalite düşük |
| LibreTranslate (kendi sunucumuz) | Sınırsız, açık kaynak | İmaj ve modeller çok büyük, CI'daki Docker işi yavaşlar; Türkçe kalitesi orta |
| TR→EN sözlük (eski yol) | Bağımlılık yok | Sözlükte olmayan kelime aranamıyor, sonuç adları İngilizce kalıyor |

**Karar etmenleri:** Türk yemeklerinin bulunabilmesi, ücretsiz olması, lisansa uygunluk, anahtarların sunucuda kalması, dış servis çökünce aramanın sürmesi.

**Karar:**

1. PlateRank, NutritionDataPort'un üçüncü adapter'ı olur (PlateRankAdapter + PlateRankClient). Sorguda yalnızca yayındaki ve lisansı uygun satırlar istenir: McDonald's satırları PlateRank lisansının dışında, Open Food Facts satırları ODbL (aynı lisansla paylaşma şartı) olduğu için alınmaz. Arayüzde "PlateRank (platerank.dev), CC BY 4.0" atfı gösterilir.
2. Porsiyonun gramı yoksa sonuç `ValuesPerPortion = true` ile döner (değerler 1 porsiyon için). Aktarırken diyetisyen gramı girer, FoodSearchService değerleri 100 g'a çevirir. Değerler tahmin olduğu için aktarma formunda "kontrol et" uyarısı çıkar.
3. Sözlük kaldırılır. Yeni giden port ITranslator, DeepLTranslationAdapter ile uygulanır. NutritionDataPort'a kaynağın dili (`LanguageCode`) eklenir. Dili Türkçe olmayan kaynak (USDA) için FoodSearchService sorguyu o dile, gelen adları Türkçeye çevirir. Çevirmen hata fırlatmaz: anahtar yoksa, kota dolduysa (456) ya da servis yanıt vermezse metinler olduğu gibi döner. Çeviriler 24 saat bellekte tutulur.
4. FoodSearchService artık tek dış kaynak değil bir liste alır; kaynaklar aynı anda sorulur. Biri çalışmazsa diğerlerinin sonuçları "X kaynağına şu an ulaşılamıyor" uyarısıyla döner (FR-D4). Her dış kaynağın kendi saatlik kotası vardır (ApiQuotaCounter, anahtarla kayıtlı iki örnek).
5. Anahtarlar yalnızca sunucuda: user-secrets (`PlateRank:ApiKey`, `DeepL:ApiKey`), Docker'da `.env` (`PLATERANK_API_KEY`, `DEEPL_API_KEY`). PlateRank anahtarı sitesinde herkese açık yazıyor ama yine de diğerleri gibi koda girmez. Anahtarı boş olan özellik kapalı kalır.

**Sonuçlar:**

- ➕ Türk yemekleri Türkçe adlarıyla aranıp aktarılabiliyor.
- ➕ USDA'da sözlükte olmayan kelimeler de aranabiliyor; sonuç adları Türkçe geliyor.
- ➕ Adapter deseni üç kaynakla gösteriliyor; dördüncü kaynak için yeni adapter + Program.cs'te bir kayıt yeterli (NFR-B1). Bu değişiklikte Application'a dokunmak zorunda kaldık çünkü FoodSearchService tek dış kaynak alıyordu; artık liste aldığı için sonraki kaynaklarda gerekmiyor.
- ➖ PlateRank değerlerinin çoğu tahmin ve sitesi klinik kullanım için önermiyor. Bu yüzden değerler doğrudan plana girmiyor; diyetisyen kontrol edip gramını girerek aktarıyor.
- ➖ DeepL'in ücretsiz kotası ayda 500.000 karakter; bellekteki çeviriler ve aramanın en az 2 karakter olması kotayı koruyor. Kota dolarsa arama çevirisiz devam ediyor.
- ➖ İki yeni dış servis demek; ikisi de çalışmazsa arama yine yerel kayıtlarla sürüyor.
