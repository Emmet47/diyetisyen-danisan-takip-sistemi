# docs — Proje belgeleri

Klasörler gereksinim belgesinin "10.2 Teknik çıktılar tablosu"ndaki "Repodaki yeri" sütunuyla aynıdır.

| Yer | İçerik | Belgedeki şekil |
| --- | --- | --- |
| `gereksinimler.md` | Fonksiyonel gereksinimler, iş kuralları, NFR, kalite senaryoları, KVKK | Bölüm 2 |
| `uml/uc-1 … uc-4` (`.mmd`) | Kullanım durumu diyagramları | Şekil 3.1–3.4 |
| `uml/cd-1`, `uml/cd-2` (`.mmd`) | Sınıf diyagramları: alan modeli | Şekil 4.1–4.2 |
| `uml/cd-3` (`.dot`) | Enum ve değer nesneleri | Şekil 4.3 |
| `uml/cd-4` (`.mmd`) | Portlar ve adapter'lar | Şekil 4.4 |
| `uml/p-1 … p-7` | Desenler: Composite, Builder, Strategy, Prototype, Adapter, Repository, Observer | Şekil 4.5–4.11 |
| `uml/sd-1 … sd-9` (`.mmd`) | Sıra diyagramları | Şekil 5.1–5.9 |
| `uml/er-1`, `uml/er-2` (`.mmd`) | ER diyagramları | Şekil 6.1–6.2 |
| `uml/ar-1-hexagonal-bilesen.dot` | Bileşen (port/adapter) diyagramı | Şekil 7.1 |
| `uml/png/`, `uml/svg/` | Aynı diyagramların çizilmiş halleri (belgeye eklenenler PNG) | |
| `db/schema.sql` | Referans veritabanı şeması, 17 tablo | Bölüm 6 |
| `db/constraints_test.py`, `db/test_sonucu.txt` | 33 kısıt testi ve son çalıştırmanın çıktısı | Bölüm 9.4 |
| `adr/` | Mimari karar kayıtları (ADR-0001…0006) | Bölüm 7.7 |
| `api.md` | REST API dokümantasyonu, örnek istek/yanıtlar, rol-yetki matrisi | Bölüm 8 |

## Kod yazarken

- Sınıf, arayüz ve metot adları `uml/cd-*`, `uml/p-*` diyagramlarındakiyle **aynı** olmalı (ör. `DdtsDbContext`, `AggregateRoot`, `IUnitOfWork.ExecuteInTransactionAsync`).
- Bir use case'in akışı ilgili sıra diyagramına (`uml/sd-*`) uymalı; farklı yapmak gerekirse önce diyagramı güncelleyin.
- Tablolar EF Core migration'larıyla üretilir; her migration'dan sonra kısıtlar `db/schema.sql` ile karşılaştırılır.
  EXCLUDE kısıtı (çift randevu engeli) EF Core modelinden üretilemez; migration'a `migrationBuilder.Sql(...)` ile elle eklenir.
- Demo verileri sahtedir (ör. "Dyt. Ayşe Örnek", `@ornek.test` e-postaları); gerçek kişi ya da sağlık bilgisi kullanılmaz.

## Diyagramları yeniden çizmek

Gerekenler: Node.js (npx için) ve Graphviz (`dot` komutu). Bir kaynağı (`.mmd`/`.dot`) değiştirdikten sonra:

```bash
cd docs/uml/araclar
./ciz.sh        # tüm PNG ve SVG'leri yeniden üretir (Windows'ta WSL veya Git Bash ile)
```

`cd-3` ve `p-6` kaynakları `araclar/gen_cd3.py` ve `araclar/gen_p6.py` ile üretilir; onları değiştirirken `.dot` dosyasını değil betiği düzenleyin.

## Veritabanı kısıt testleri

Gerekenler: PostgreSQL 16 veya üstü, Python 3 ve `pip install "psycopg[binary]"`.

```bash
cd docs/db
python3 constraints_test.py "host=localhost port=5432 user=ddts password=<POSTGRES_PASSWORD>"
```

Betik geçici bir `ddts_sade_test` veritabanı açar, şemayı yükler, 33 testi çalıştırır ve sonunda siler.
Docker'daki `postgres` servisine de bağlanabilir (`.env`'deki kullanıcı ve parolayla).

## API dokümantasyonu

API çalışırken deneme arayüzü (Scalar): http://localhost:8080/scalar

Teslim için OpenAPI belgesini dışa aktarmak:

```bash
curl --create-dirs http://localhost:8080/openapi/v1.json -o docs/openapi.json
```
