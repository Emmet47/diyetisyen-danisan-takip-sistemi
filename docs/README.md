# docs — Proje belgeleri

Klasörler gereksinim belgesinin "10.2 Teknik çıktılar tablosu"ndaki "Repodaki yeri" sütunuyla aynıdır.

| Yer | İçerik |
| --- | --- |
| `gereksinimler.md` | Gereksinimler, iş kuralları, NFR ve kalite senaryoları (Bölüm 2) |
| `uml/uc-*.mmd` | Kullanım durumu diyagramları (Şekil 3.1–3.4) |
| `uml/cd-*.mmd` | Sınıf diyagramları: alan modeli, portlar (Şekil 4.1–4.4) |
| `uml/p-*.mmd` | Tasarım deseni diyagramları (Şekil 4.5–4.11) |
| `uml/sd-*.mmd` | Sıra diyagramları (Şekil 5.1–5.9) |
| `uml/er-*.mmd` | ER diyagramları (Şekil 6.1–6.2) |
| `uml/ar-1-hexagonal-bilesen.dot` | Bileşen (port/adapter) diyagramı (Şekil 7.1) |
| `adr/0001…0006` | Mimari karar kayıtları (Bölüm 7.7) |
| `db/schema.sql` | Veritabanı şeması (Bölüm 6) |
| `db/constraints_test.py` | Veritabanı kısıt testleri, 33/33 (Bölüm 9.4) |
| `api.md` | REST API dokümantasyonu, örnek istek/yanıtlar, rol-yetki matrisi (Bölüm 8) |

`DDTS_Sade_UML_Paketi.zip` içindeki dosyalar bu adlarla yukarıdaki klasörlere konur.

## Diyagramları çizmek

- `.mmd` dosyaları Mermaid CLI ile: `npx -p @mermaid-js/mermaid-cli mmdc -i uml/cd-1.mmd -o uml/cd-1.svg`
- `.dot` dosyası Graphviz ile: `dot -Tsvg uml/ar-1-hexagonal-bilesen.dot -o uml/ar-1-hexagonal-bilesen.svg`

## API dokümantasyonu

API çalışırken deneme arayüzü (Scalar): http://localhost:8080/scalar

Teslim için OpenAPI belgesini dışa aktarmak:

```bash
curl --create-dirs http://localhost:8080/openapi/v1.json -o docs/openapi.json
```
