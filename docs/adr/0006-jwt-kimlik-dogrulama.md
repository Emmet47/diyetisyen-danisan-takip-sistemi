# ADR-0006: Kimlik doğrulama için JWT

**Durum:** Kabul edildi · 10.11.2026 · **İlgili:** FR-A2, FR-A4, NFR-G2

**Bağlam:** Hem web (React) hem mobil (Flutter) istemci aynı REST API'yi kullanıyor. Güvenli oturum ve rol kontrolü isteniyor.

**Değerlendirilen seçenekler:**

| Seçenek | Artı | Eksi |
| --- | --- | --- |
| Çerez (cookie) tabanlı oturum | Tarayıcıda olgun | Mobilde ek iş; CSRF (siteler arası istek sahteciliği) koruması gerekir |
| **JWT (Authorization: Bearer) ✓** | Web ve mobil için tek yöntem; ASP.NET Core'da hazır | Süresi dolmadan iptal edilemez; istemcide güvenli saklanmalı |

**Karar etmenleri:** İki istemci için tek yöntem, basitlik, güvenlik.

**Karar:** Girişte 60 dakika geçerli JWT vereceğiz; rol ve diyetisyen/danışan kimliği token'da claim olarak taşınacak. Mobilde token'ı flutter\_secure\_storage ile güvenli depoda, web'de sessionStorage'da tutacağız; süre dolunca yeniden giriş istenecek.

**Sonuçlar:**

- ➕ Tek kimlik doğrulama yolu; sunucuda oturum tablosu yok.
- ➖ Pasife alınan kullanıcının token'ı süresi bitene kadar geçerli olurdu; bunu her istekte hesabın aktif olup olmadığını kontrol ederek kapıyoruz.
- ➖ İlk sürümde yenileme (refresh) token'ı yok; kullanıcı saatte bir yeniden giriş yapıyor.
