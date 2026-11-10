# 2. Gereksinimler

Gereksinimleri hocamızın A–L modüllerine göre gruplandırdık. Her satırda gereksinimi nasıl doğrulayacağımızı da yazdık; test kodları (UT = birim testi, IT = API entegrasyon testi, DB = veritabanı kısıt testi, KT = kabul testi) 9. bölümde listeleniyor. Böylece her gereksinim test edilebilir oluyor.

## 2.1 Fonksiyonel gereksinimler

**A. Kimlik ve Yetkilendirme**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-A1 | Diyetisyen ad-soyad, e-posta ve parola ile hesap açar ve bir abonelik paketi seçer. | KT-01 |
| FR-A2 | Diyetisyen, danışan ve yönetici e-posta + parola ile giriş yapar; başarılı girişte 60 dakika geçerli JWT alır. Parolalar geri çözülemeyecek şekilde hash'lenerek saklanır. | IT-04 |
| FR-A3 | “Parolamı unuttum” diyen kullanıcıya 30 dakika geçerli, tek kullanımlık bir bağlantı e-postalanır; kullanıcı yeni parolasını belirler. Danışan daveti de aynı mekanizmayla çalışır. | IT-14 |
| FR-A4 | Her istekte rol (Diyetisyen / Danışan / Yönetici) ve sahiplik kontrol edilir. Rol uymuyorsa 403, kayıt kullanıcıya ait değilse 404 döner. | IT-01, IT-02 |
| FR-A5 | Önemli işlemler (giriş, danışan ekleme/güncelleme/silme, ölçüm ekleme, plan yayımlama, randevu durum değişikliği, abonelik değişikliği) denetim kaydına (AuditLog) yazılır. | IT-12 |

**B. Danışan Profil Yönetimi**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-B1 | Diyetisyen danışan ekler: ad-soyad, e-posta, telefon, doğum tarihi, cinsiyet, hedef (kilo vermek / korumak / almak + hedef kilo), beslenme tercihleri ve alerji notu. “Danışandan açık rıza alındı” onayı zorunludur. Danışana mobil uygulama için davet e-postası gider. | IT-03, KT-02 |
| FR-B2 | Diyetisyen danışanın alerji/kısıt listesini tutar. Listedeki her besin “Alerji” (plana eklenemez) ya da “Sevmiyor” (uyarı verir) olarak işaretlenir. | UT-05 |
| FR-B3 | Diyetisyen danışan bilgilerini günceller. Her değişiklik eski/yeni değer, kim ve ne zaman bilgisiyle değişiklik geçmişinde listelenir. | IT-12 |
| FR-B4 | Diyetisyen danışanı pasife alır (paket sayımına girmez, danışan giriş yapamaz, veriler durur) ya da kalıcı olarak siler (ölçüm, plan, öğün kaydı, randevu ve fotoğraflarıyla birlikte). Pasif danışan tekrar aktifleştirilirken paket limiti yeniden kontrol edilir. | DB-33, IT-13 |

**C. Ölçüm ve Gelişim Takibi**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-C1 | Diyetisyen tarih bazlı ölçüm ekler: kilo, boy ve bel çevresi zorunlu; kalça çevresi ve vücut yağ oranı isteğe bağlı. Aynı gün için tek ölçüm girilir. | DB-20, DB-21 |
| FR-C2 | Sistem ölçüm geçmişini tarih sırasıyla listeler ve her ölçümde bir öncekine göre farkı gösterir. | UT-13 |
| FR-C3 | Sistem kilo ve bel çevresi için eğilim grafiği çizer. Hedef kilo grafikte yatay çizgi olarak görünür ve “hedefe kalan kilo” hesaplanır. | KT-03 |
| FR-C4 | Danışan mobil uygulamada kendi ölçüm geçmişini ve grafiğini görür. | KT-07 |

**D. Besin Veritabanı**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-D1 | Diyetisyen besinleri ada göre arar ve kaynağa göre (Yerel / USDA) filtreler. Sonuçlarda porsiyon, 100 g'daki enerji, protein, karbonhidrat ve yağ değerleri ile veri kaynağı ve güncellik tarihi görünür. | KT-06 |
| FR-D2 | Diyetisyen yerel besin ekler, düzenler, siler (yalnızca kendi kayıtlarını). Ortak (hazır yüklenen) besinleri herkes kullanır ama kimse düzenleyemez. Bir plan ya da şablonda kullanılan besin silinemez. | DB-16 |
| FR-D3 | Diyetisyen dış kaynakta (USDA FoodData Central) arama yapar; seçtiği sonucu değerlerini kontrol ederek yerel veritabanına aktarır. Dış kaynağın yanıtı Adapter ile sistemin standart NutritionItem modeline çevrilir. | DB-19, KT-06 |
| FR-D4 | Dış API erişilemezse (zaman aşımı, hata, kota dolu) sistem çökmez; kullanıcıya uyarı gösterip yerel kayıtlarla devam eder. Yerel kayıtları, bir diyetisyen tarafından girildiği ya da kontrol edilip kaydedildiği için “doğrulanmış kayıt” kabul ediyoruz. | IT-07 |

**E. Öğün ve Beslenme Planı**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-E1 | Diyetisyen danışan için günlük (1 gün) ya da haftalık (7 gün) plan hazırlar: öğün adı ve saati, besin ve porsiyon miktarı seçer. Plan adım adım kurulur ve kaydedilmeden önce doğrulanır (Builder). | UT-05 |
| FR-E2 | Bir besin satırına alternatif eklenebilir (ör. “2 yumurta yerine 2 dilim peynir”). Alternatifler toplam hesabına katılmaz. | UT-04, DB-14 |
| FR-E3 | Plan durumları: Taslak → Yayımlandı → Arşiv. Yalnızca taslak düzenlenir ya da silinir. Yayımlanan plan değiştirilemez; değişiklik gerekirse yeni plan hazırlanır. | UT-12 |
| FR-E4 | Danışanın aynı anda tek yayımlanmış planı olur. Yeni plan yayımlanınca eskisi otomatik arşivlenir ve danışana bildirim gider. Alerjen besin içeren plan yayımlanamaz. | DB-07, DB-08, IT-08 |
| FR-E5 | Danışan mobil uygulamada yayımlanmış planını ve bugünkü öğünlerini görür. | KT-07 |

**F. Plan Şablonları**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-F1 | Diyetisyen danışana bağlı olmayan, tekrar kullanılabilir plan şablonları oluşturur, düzenler ve siler. | KT-05 |
| FR-F2 | Diyetisyen bir şablonu seçtiği danışana kopyalar. Sistem şablonun bağımsız (derin) kopyasını taslak plan olarak oluşturur (Prototype). Kopyada danışanın kısıtlarına takılan besinler uyarı olarak listelenir. | UT-07 |
| FR-F3 | Kopya üzerindeki porsiyon ve içerik değişiklikleri özgün şablonu değiştirmez. | UT-07, IT-06 |

**G. Öğün Günlüğü ve Fotoğraf**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-G1 | Danışan bugünkü planlı öğününü “Yedim / Kısmen yedim / Yemedim” olarak işaretler ve isteğe bağlı not yazar. Bir öğün aynı gün için bir kez kaydedilir, sonra güncellenebilir. | DB-22 |
| FR-G2 | Danışan öğün kaydına bir fotoğraf ekler: yalnızca JPEG/PNG, en fazla 5 MB. Fotoğraf herkese kapalı depoda saklanır; görüntülemek için 10 dakika geçerli özel bağlantı üretilir. | UT-10, IT-09, IT-10 |
| FR-G3 | Diyetisyen danışanın planlanan ve gerçekleşen öğünlerini gün gün yan yana görür. | KT-07 |

**H. Enerji ve Besin Hesaplama**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-H1 | Diyetisyen formülü (Mifflin-St Jeor ya da Harris-Benedict) ve parametreleri (aktivite katsayısı 1,2–1,9; isteğe bağlı kcal düzeltmesi) seçer. Sistem danışanın son ölçümü ve yaşıyla BMH'yi, günlük enerji ihtiyacını ve hedef enerjiyi hesaplar (Strategy). | UT-01, UT-02, UT-03 |
| FR-H2 | Sistem her öğün, gün ve plan için toplam enerjiyi, protein/karbonhidrat/yağ miktarını ve makro dağılımını (%) hesaplar (Composite). | UT-04 |
| FR-H3 | Sonuçların yanında “Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.” uyarısı gösterilir. | UT-03, KT-05 |

**I. Randevu Yönetimi**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-I1 | Diyetisyen haftalık çalışma saatlerini girer (her gün için başlangıç–bitiş). Sistem bu saatleri 30 dakikalık slotlara böler, dolu ve geçmiş slotları çıkarıp uygun saatleri listeler. | UT-09 |
| FR-I2 | Danışan uygun bir slota randevu talep eder (Talep Edildi). Diyetisyen talebi onaylar (Onaylandı) ya da iptal eder; isterse doğrudan onaylı randevu da oluşturur. | UT-08, KT-08 |
| FR-I3 | Diyetisyen ve danışan randevuyu erteleyebilir ya da iptal edebilir. Görüşmeden sonra diyetisyen randevuyu “Tamamlandı” yapar. Randevuların durumu listede takip edilir. | UT-08 |
| FR-I4 | Aynı diyetisyenin çakışan iki aktif randevusu olamaz. Aynı slota aynı anda gelen iki talepten yalnızca biri kaydedilir, diğeri 409 hatası alır. | DB-26…DB-32, IT-05 |

**J. Hatırlatma ve Bildirimler**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-J1 | Sistem şu olaylarda bildirim üretir: randevu talebi (diyetisyene), onay/iptal/erteleme (karşı tarafa), randevudan 24 saat önce hatırlatma, yeni plan yayımlanması, saat 20.00'de o gün kaydedilmemiş öğün hatırlatması, son ölçümden 14 gün sonra ölçüm hatırlatması. | IT-08, IT-11 |
| FR-J2 | Danışan dört bildirim türünü (randevu, öğün, ölçüm, plan) mobil uygulamadan açıp kapatır. Kapalı türde bildirim gönderilmez (“izinli bildirim”). | UT-11 |
| FR-J3 | Bildirimler uygulama içinde okundu/okunmadı olarak listelenir; izin verildiyse telefona anlık bildirim (push) olarak da gider. Aynı hatırlatma ikinci kez gönderilmez. | DB-30, IT-11 |

**K. Raporlama ve Danışan Paneli**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-K1 | Diyetisyen her danışan için bir panel görür: son ölçümler, kilo ve bel eğilim grafikleri, plan uyumu (%), öğün kayıt sıklığı. | KT-03 |
| FR-K2 | Diyetisyen seçtiği tarih aralığı için dönemsel ilerleme raporunu PDF olarak indirir. | KT-03 |
| FR-K3 | Hesaplama tanımları: **plan uyumu** = (Yedim + 0,5 × Kısmen yedim) ÷ dönemdeki planlı öğün sayısı × 100; **kayıt sıklığı** = en az bir öğün kaydı olan gün sayısı ÷ dönemdeki gün sayısı × 100. | UT-13 |

**L. Abonelik ve Paket Yönetimi**

| Kod | Gereksinim | Doğrulama |
| --- | --- | --- |
| FR-L1 | Yönetici paketleri tanımlar ve düzenler: ad, danışan limiti, aylık ücret, satışta mı. Demo paketleri: Başlangıç (10 danışan), Standart (30), Profesyonel (100). | KT-09 |
| FR-L2 | Yeni danışan eklenirken (ya da pasif danışan aktifleştirilirken) aktif danışan sayısı paket limitiyle karşılaştırılır. Limit doluysa işlem engellenir ve “paketini yükselt” uyarısı çıkar. Limitin %80'ine gelince panelde sarı uyarı görünür. | UT-06, IT-03 |
| FR-L3 | Diyetisyen paketini yükseltir ya da düşürür. Düşürmede aktif danışan sayısı yeni limitten fazlaysa işlem reddedilir. | UT-06 |
| FR-L4 | Abonelik durumu (Aktif / Süresi Doldu / İptal) ve bitiş tarihi gösterilir. Ödeme simülasyonla yapılır, kart bilgisi alınmaz. Süresi dolan abonelikte yeni danışan ve yeni plan eklenemez, mevcut veriler okunabilir. | UT-06 |
| FR-L5 | Yönetici diyetisyen hesaplarını listeler, aktif/pasif yapar; her diyetisyenin paketini, kullanımını (ör. 8/10) ve denetim kayıtlarını (sağlık verisi olmadan) görür. | IT-12, KT-09 |

## 2.2 Temel iş kuralları

Bazı kurallar birden fazla gereksinimi etkilediği için onları ayrıca topladık. “Nerede?” sütunu kuralın kodda hangi sınıfta ya da veritabanında hangi kısıtta uygulandığını gösteriyor.

| Kod | Kural | Nerede? | Test |
| --- | --- | --- | --- |
| BR-01 | Aktif danışan sayısı paket limitine ulaştıysa yeni danışan eklenemez; abonelik aktif değilse yeni danışan ve yeni plan eklenemez. | Subscription.EnsureCanAddClient(), ClientService | UT-06, IT-03 |
| BR-02 | Paket düşürmede aktif danışan sayısı yeni limiti aşamıyor olmalı. | SubscriptionService | UT-06 |
| BR-03 | Danışanın tek yayımlanmış planı olur; yeni yayım eskisini arşivler. | NutritionPlan + kısmi tekil indeks | DB-07, IT-08 |
| BR-04 | Yalnızca Taslak plan düzenlenir; arşivlenen plan geri açılmaz. | NutritionPlan + CHECK kısıtı | UT-12, DB-09 |
| BR-05 | Danışanın “Alerji” listesindeki besin plana eklenemez, böyle bir plan yayımlanamaz; “Sevmiyor” listesindeki besin uyarı verir. | NutritionPlanBuilder, PlanService | UT-05 |
| BR-06 | Alternatif satırlar toplama katılmaz; alternatif yalnızca aynı öğündeki bir ana satıra bağlanabilir. | Meal.CalculateTotals() + bileşik FK | UT-04, DB-15 |
| BR-07 | Şablondan alınan kopya bağımsızdır; kopyada yapılan değişiklik şablonu etkilemez. | Meal.Clone(), PlanTemplate.CreatePlanFor() | UT-07, IT-06 |
| BR-08 | Aynı diyetisyenin Talep Edildi ya da Onaylandı durumundaki randevuları zaman olarak kesişemez. Randevu çalışma saati içinde, bir slotun başında ve gelecekte olmalı. | AvailabilityService + EXCLUDE kısıtı | UT-09, DB-26, IT-05 |
| BR-09 | Randevu geçişleri: Talep Edildi → Onaylandı (diyetisyen); Talep Edildi/Onaylandı → İptal (iki taraf). Danışan ertelerse randevu yeniden Talep Edildi olur, diyetisyen ertelerse Onaylandı kalır. Tamamlandı yalnızca onaylı ve saati geçmiş randevuda verilir. | Appointment | UT-08 |
| BR-10 | Fotoğraf yalnızca JPEG/PNG (dosya imzasına bakılır, uzantıya değil), en fazla 5 MB ve öğün kaydı başına bir tane. Bağlantı 10 dakika geçerli. | PhotoInfo, MealLogService + CHECK | UT-10, DB-23, IT-09 |
| BR-11 | Öğün kaydı yalnızca danışanın yayımlanmış planındaki bir öğün için, bugün ya da geçmiş bir gün için ve günde bir kez yapılır. | MealLogService + tekil kısıt | DB-22 |
| BR-12 | Danışanın kapattığı türde bildirim üretilmez; aynı hatırlatma (aynı dedupKey) iki kez üretilmez. | Client.AllowsNotification(), NotificationService + tekil kısıt | UT-11, DB-30 |

## 2.3 Fonksiyonel olmayan gereksinimler (NFR)

Derste gördüğümüz gibi “hızlı olsun, güvenli olsun” gibi belirsiz cümleler yerine ölçülebilir hedefler yazdık.

| Kod | Nitelik | Gereksinim | Ölçüt / doğrulama |
| --- | --- | --- | --- |
| NFR-G1 | Güvenlik – veri izolasyonu | Diyetisyen yalnızca kendi danışanlarının verisine erişir. Her sorgu diyetisyen kimliğiyle (tenant) filtrelenir. | Başka diyetisyenin kaydına giden tüm istekler 404 döner (IT-01, %100 geçmeli). |
| NFR-G2 | Güvenlik – oturum ve parola | Tüm trafik HTTPS; parolalar PBKDF2 ile hash'lenir (ASP.NET Core PasswordHasher); JWT 60 dakika geçerli; giriş ucuna IP başına dakikada en fazla 5 deneme. | 6. denemede 429 (IT-04); HTTP istekleri HTTPS'e yönlenir. |
| NFR-G3 | Güvenlik – fotoğraf | Yalnızca JPEG/PNG, en fazla 5 MB; depo (bucket) herkese kapalı; görüntüleme bağlantısı 10 dakika geçerli. | IT-09, IT-10 |
| NFR-G4 | Güvenlik – denetim izi | FR-A5'teki işlemlerin hepsi kimin, ne zaman, ne yaptığıyla kaydedilir. | IT-12 |
| NFR-G5 | Güvenlik – gizli anahtarlar | USDA API anahtarı, JWT imza anahtarı ve FCM kimlik bilgileri yalnızca sunucuda (ortam değişkeni / user-secrets) durur; koda, Git'e ve istemcilere girmez. | Kod incelemesi; istemciler USDA'yı hiç doğrudan çağırmaz. |
| NFR-E1 | Hata toleransı | Dış besin API'si yanıt vermezse arama en geç 4 saniyede (3 sn zaman aşımı + yerel arama) yerel sonuçlarla döner; kullanıcı 500 hatası görmez. | IT-07 |
| NFR-P1 | Performans | Danışan listesi ve yerel besin araması, demo verisinde (2 diyetisyen, 60 danışan, 5.000 besin) ve 20 eşzamanlı kullanıcıyla p95 < 500 ms. | Basit yük testi (k6 aracıyla). |
| NFR-B1 | Bakım – değiştirilebilirlik | Yeni besin kaynağı = yeni bir adapter sınıfı + DI kaydı; yeni enerji formülü = yeni bir strateji sınıfı. Domain ve Application projelerinde değişiklik gerekmez. | Kod incelemesi + AT-01 |
| NFR-T1 | Test edilebilirlik | Domain projesi EF Core, ASP.NET Core ya da AWS SDK gibi altyapı paketlerine bağlanmaz; birim testleri veritabanı olmadan 10 saniyenin altında biter. | AT-01, CI süresi |
| NFR-K1 | Gizlilik – KVKK | 2.5'teki maddeler uygulanır. | Kontrol listesi + DB-33, IT-12, IT-13 |
| NFR-D1 | Demo verisi | Demo ve test verileri sahtedir (Bogus kütüphanesiyle üretilir); gerçek kişi ya da sağlık bilgisi kullanılmaz. | Seed betiklerinin incelenmesi |

## 2.4 Kalite senaryoları

En önemli dört NFR'yi dersteki altı parçalı kalite senaryosu kalıbıyla yazdık: **Kaynak** (kim/ne?), **Uyarıcı** (ne oldu?), **Ortam** (hangi koşulda?), **Artefakt** (sistemin hangi parçası?), **Yanıt** (sistem ne yapar?), **Yanıt ölçüsü** (nasıl ölçeriz?).

| Nitelik | Kaynak | Uyarıcı | Ortam | Artefakt | Yanıt | Yanıt ölçüsü |
| --- | --- | --- | --- | --- | --- | --- |
| Erişilebilirlik / hata toleransı | USDA servisi | Yanıt vermiyor ya da 429 dönüyor | Normal çalışma | Besin arama (FoodSearchService) | Hata loglanır, kullanıcıya uyarı gösterilir, yerel sonuçlar döner | ≤ 4 sn, HTTP 200, 0 çökme |
| Güvenlik | Diyetisyen B | A'nın danışanının id'sini adrese yazıyor | Geçerli oturumla, normal çalışma | Danışan, ölçüm, plan ve fotoğraf uçları | İstek reddedilir, kaydın varlığı bile belli edilmez | %100 istekte 404 |
| Veri bütünlüğü | İki danışan | Aynı diyetisyenin aynı slotuna aynı anda talep gönderiyor | Eşzamanlı istekler | Randevu servisi + veritabanı | Biri kaydedilir, diğerine “bu saat doldu” denir | 1 × 201, 1 × 409, 0 çift kayıt |
| Bakım yapılabilirlik | Geliştirici | İkinci bir dış besin kaynağı eklemek istiyor | Geliştirme zamanı | NutritionDataPort | Yeni adapter yazılır, Program.cs'te kaydedilir | Domain/Application'da 0 satır değişiklik, ≤ 1 geliştirici-günü |

## 2.5 KVKK'ya uyum için yaptıklarımız

Kilo, ölçüm, alerji gibi bilgiler sağlık verisi sayılıyor; KVKK'da sağlık verisi “özel nitelikli kişisel veri”. Hocamızın istediği beş başlığı tasarımda şöyle karşıladık:

1. **Aydınlatma:** Danışan eklenirken diyetisyen panelinde, danışanın ilk girişinde de mobil uygulamada aydınlatma metni (hangi veriyi, hangi amaçla, ne kadar süre tuttuğumuz ve hakları) gösterilir.
2. **Hukuki dayanak:** Sağlık verisini danışanın **açık rızasına** dayanarak işliyoruz. Diyetisyen rızayı aldığını işaretlemeden danışan eklenemez; rıza zamanı `clients.kvkk_consent_at` alanında saklanır.
3. **Veri minimizasyonu:** Yalnızca işe yarayan alanları tutuyoruz: TC kimlik no, adres, kan grubu gibi bilgileri almıyoruz. Bildirim metinlerine ve log kayıtlarına sağlık verisi yazmıyoruz (örnek bildirim: “Yeni planın hazır”). Bu aynı zamanda push bildirimlerinin yurt dışındaki bir servis (FCM) üzerinden gitmesinden doğan riski azaltıyor.
4. **Saklama ve silme:** İlişki bitince danışan pasife alınır ve giriş yapamaz. Danışan silinmeyi isterse diyetisyen “kalıcı sil” ile bütün kayıtları (ölçüm, plan, öğün kaydı, randevu, bildirim) ve depodaki fotoğrafları siler; denetim kayıtlarında ise yalnızca “kim, ne zaman, hangi işlem” kalır, değer ayrıntıları temizlenir. Kanundaki başvuru süresine uygun olarak bu talebi en geç 30 gün içinde karşılamayı hedefliyoruz.
5. **Erişim politikası:** Sağlık verisini yalnızca danışanın kendisi ve bağlı olduğu diyetisyen görür. Platform yöneticisi danışan verilerine erişemez; yalnızca hesapları, paketleri, kullanım sayılarını ve ayrıntısız denetim kayıtlarını görür (rol-yetki matrisi: 8.4).
