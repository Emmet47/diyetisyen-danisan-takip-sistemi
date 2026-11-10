"""DDTS sade şema - veritabanı kısıt testleri.

Kullanım:  python3 constraints_test.py "host=localhost port=5432 user=postgres password=..."
Her test kendi işleminde (transaction) çalışır ve sonunda geri alınır (rollback).
Beklenen sonuç: 'ok' ya da PostgreSQL hata kodu (SQLSTATE):
  23505 = tekillik ihlali, 23503 = yabancı anahtar ihlali,
  23514 = CHECK ihlali,     23P01 = EXCLUDE (çakışma) ihlali
"""
import sys
import threading
import uuid

import psycopg

DSN = sys.argv[1] if len(sys.argv) > 1 else "host=/tmp port=5544 user=postgres"
DB = "ddts_sade_test"


def new_id():
    return str(uuid.uuid4())


# --- test veritabanını sıfırdan kur -------------------------------------------
with psycopg.connect(DSN + " dbname=postgres", autocommit=True) as c:
    c.execute(f"DROP DATABASE IF EXISTS {DB}")
    c.execute(f"CREATE DATABASE {DB}")
CONN = DSN + f" dbname={DB}"
with psycopg.connect(CONN, autocommit=True) as c:
    c.execute(open(__file__.replace("constraints_test.py", "schema.sql"), encoding="utf-8").read())

# --- ortak örnek veri (sahte kişiler, gerçek sağlık verisi yok) ----------------
I = {k: new_id() for k in [
    "uA", "uB", "uCA", "uCB", "dA", "dB", "pkg1", "pkg2", "subA", "subB", "cA", "cB",
    "food1", "food2", "tpl", "tMeal", "tItem", "plan1", "pMeal1", "pMeal2", "pItem1", "pItem2", "appt1"]}

FIXTURE = f"""
INSERT INTO users (id, email, password_hash, role) VALUES
  ('{I['uA']}',  'ayse.dyt@ornek.test',   'hash', 'Dietitian'),
  ('{I['uB']}',  'burak.dyt@ornek.test',  'hash', 'Dietitian'),
  ('{I['uCA']}', 'danisan.a@ornek.test',  NULL,   'Client'),
  ('{I['uCB']}', 'danisan.b@ornek.test',  NULL,   'Client');
INSERT INTO dietitians (id, user_id, full_name) VALUES
  ('{I['dA']}', '{I['uA']}', 'Dyt. Ayşe Örnek'),
  ('{I['dB']}', '{I['uB']}', 'Dyt. Burak Örnek');
INSERT INTO subscription_plans (id, name, max_clients, monthly_price) VALUES
  ('{I['pkg1']}', 'Başlangıç', 10, 299), ('{I['pkg2']}', 'Standart', 30, 599);
INSERT INTO subscriptions (id, dietitian_id, subscription_plan_id, status, start_date, end_date) VALUES
  ('{I['subA']}', '{I['dA']}', '{I['pkg1']}', 'Active', current_date, current_date + 30),
  ('{I['subB']}', '{I['dB']}', '{I['pkg1']}', 'Active', current_date, current_date + 30);
INSERT INTO clients (id, dietitian_id, user_id, full_name, birth_date, sex, goal_type, kvkk_consent_at) VALUES
  ('{I['cA']}', '{I['dA']}', '{I['uCA']}', 'Deneme Danışan A', '1995-01-01', 'Female', 'LoseWeight', now()),
  ('{I['cB']}', '{I['dB']}', '{I['uCB']}', 'Deneme Danışan B', '1990-06-15', 'Male',   'Maintain',   now());
INSERT INTO food_items (id, dietitian_id, name, portion_name, portion_grams,
                        energy_kcal_per_100g, protein_g_per_100g, carbs_g_per_100g, fat_g_per_100g) VALUES
  ('{I['food1']}', NULL, 'Haşlanmış yumurta', '1 adet', 50, 155, 12.6, 1.1, 10.6),
  ('{I['food2']}', NULL, 'Beyaz peynir', '1 dilim', 30, 264, 17.0, 2.0, 21.0);
INSERT INTO plan_templates (id, dietitian_id, name, plan_type) VALUES
  ('{I['tpl']}', '{I['dA']}', 'Kahvaltı ağırlıklı günlük şablon', 'Daily');
INSERT INTO meals (id, template_id, day_no, name, meal_time) VALUES
  ('{I['tMeal']}', '{I['tpl']}', 1, 'Kahvaltı', '08:00');
INSERT INTO meal_items (id, meal_id, food_item_id, portion_count) VALUES
  ('{I['tItem']}', '{I['tMeal']}', '{I['food1']}', 2);
INSERT INTO nutrition_plans (id, dietitian_id, client_id, title, plan_type, status, published_at) VALUES
  ('{I['plan1']}', '{I['dA']}', '{I['cA']}', 'Ekim planı', 'Daily', 'Published', now());
INSERT INTO meals (id, plan_id, day_no, name, meal_time) VALUES
  ('{I['pMeal1']}', '{I['plan1']}', 1, 'Kahvaltı', '08:00'),
  ('{I['pMeal2']}', '{I['plan1']}', 1, 'Öğle', '13:00');
INSERT INTO meal_items (id, meal_id, food_item_id, portion_count) VALUES
  ('{I['pItem1']}', '{I['pMeal1']}', '{I['food1']}', 2),
  ('{I['pItem2']}', '{I['pMeal2']}', '{I['food2']}', 1);
INSERT INTO appointments (id, dietitian_id, client_id, starts_at, ends_at, status) VALUES
  ('{I['appt1']}', '{I['dA']}', '{I['cA']}', '2026-12-01 10:00+03', '2026-12-01 10:30+03', 'Confirmed');
"""
with psycopg.connect(CONN, autocommit=True) as c:
    c.execute(FIXTURE)


def run(sql):
    """SQL'i kendi işleminde çalıştırır, sonucu döndürür ve her şeyi geri alır."""
    with psycopg.connect(CONN) as c:
        try:
            c.execute(sql)
            result = "ok"
        except psycopg.Error as e:
            result = e.sqlstate
        c.rollback()
    return result


def appt(dietitian, client, start, end, status="Requested"):
    return (f"INSERT INTO appointments (dietitian_id, client_id, starts_at, ends_at, status) "
            f"VALUES ('{I[dietitian]}', '{I[client]}', '{start}', '{end}', '{status}')")


TESTS = [
    # (kod, açıklama, sql, beklenen)
    ("DB-01", "Aynı e-posta ile ikinci hesap açılamaz",
     "INSERT INTO users (email, role) VALUES ('ayse.dyt@ornek.test', 'Dietitian')", "23505"),
    ("DB-02", "E-posta büyük harfle saklanamaz (küçük harfe çevrilmeli)",
     "INSERT INTO users (email, role) VALUES ('Yeni@Ornek.test', 'Dietitian')", "23514"),
    ("DB-03", "Bir diyetisyenin ikinci aktif aboneliği olamaz",
     f"INSERT INTO subscriptions (dietitian_id, subscription_plan_id, status, start_date, end_date) "
     f"VALUES ('{I['dA']}', '{I['pkg2']}', 'Active', current_date, current_date + 30)", "23505"),
    ("DB-04", "Paket yükseltme: eski abonelik iptal edilip yenisi aktif açılabilir",
     f"UPDATE subscriptions SET status = 'Cancelled' WHERE id = '{I['subA']}'; "
     f"INSERT INTO subscriptions (dietitian_id, subscription_plan_id, status, start_date, end_date) "
     f"VALUES ('{I['dA']}', '{I['pkg2']}', 'Active', current_date, current_date + 30)", "ok"),
    ("DB-05", "Tenant: A diyetisyeni, B'nin danışanına plan açamaz",
     f"INSERT INTO nutrition_plans (dietitian_id, client_id, title, plan_type) "
     f"VALUES ('{I['dA']}', '{I['cB']}', 'Yetkisiz plan', 'Daily')", "23503"),
    ("DB-06", "Tenant: A diyetisyeni, B'nin danışanına randevu açamaz",
     appt("dA", "cB", "2026-12-02 10:00+03", "2026-12-02 10:30+03"), "23503"),
    ("DB-07", "Danışanın ikinci yayımlanmış planı olamaz",
     f"INSERT INTO nutrition_plans (dietitian_id, client_id, title, plan_type, status, published_at) "
     f"VALUES ('{I['dA']}', '{I['cA']}', 'Kasım planı', 'Weekly', 'Published', now())", "23505"),
    ("DB-08", "Yeni plan yayımlanınca eskisi arşive alınır (izin verilen akış)",
     f"UPDATE nutrition_plans SET status = 'Archived', archived_at = now() WHERE id = '{I['plan1']}'; "
     f"INSERT INTO nutrition_plans (dietitian_id, client_id, title, plan_type, status, published_at) "
     f"VALUES ('{I['dA']}', '{I['cA']}', 'Kasım planı', 'Weekly', 'Published', now())", "ok"),
    ("DB-09", "Yayımlanmış plan yayım tarihi olmadan tutulamaz",
     f"INSERT INTO nutrition_plans (dietitian_id, client_id, title, plan_type, status) "
     f"VALUES ('{I['dA']}', '{I['cA']}', 'Hatalı', 'Daily', 'Published')", "23514"),
    ("DB-10", "Arşiv planı arşiv tarihi olmadan tutulamaz",
     f"UPDATE nutrition_plans SET status = 'Archived' WHERE id = '{I['plan1']}'", "23514"),
    ("DB-11", "Öğün hem plana hem şablona bağlanamaz",
     f"INSERT INTO meals (plan_id, template_id, day_no, name, meal_time) "
     f"VALUES ('{I['plan1']}', '{I['tpl']}', 1, 'Akşam', '19:00')", "23514"),
    ("DB-12", "Öğün sahipsiz (plansız ve şablonsuz) olamaz",
     "INSERT INTO meals (day_no, name, meal_time) VALUES (1, 'Akşam', '19:00')", "23514"),
    ("DB-13", "Öğün günü 1-7 dışında olamaz",
     f"INSERT INTO meals (plan_id, day_no, name, meal_time) VALUES ('{I['plan1']}', 8, 'Akşam', '19:00')", "23514"),
    ("DB-14", "Alternatif satır aynı öğündeki ana satıra bağlanabilir",
     f"INSERT INTO meal_items (meal_id, food_item_id, portion_count, alternative_to_item_id) "
     f"VALUES ('{I['pMeal1']}', '{I['food2']}', 2, '{I['pItem1']}')", "ok"),
    ("DB-15", "Alternatif satır başka öğündeki satıra bağlanamaz",
     f"INSERT INTO meal_items (meal_id, food_item_id, portion_count, alternative_to_item_id) "
     f"VALUES ('{I['pMeal2']}', '{I['food2']}', 2, '{I['pItem1']}')", "23503"),
    ("DB-16", "Planda kullanılan besin silinemez",
     f"DELETE FROM food_items WHERE id = '{I['food1']}'", "23503"),
    ("DB-17", "Porsiyon 0 olamaz",
     f"INSERT INTO meal_items (meal_id, food_item_id, portion_count) VALUES ('{I['pMeal1']}', '{I['food2']}', 0)",
     "23514"),
    ("DB-18", "Besinde protein+karbonhidrat+yağ 100 g'ı geçemez",
     "INSERT INTO food_items (name, portion_name, portion_grams, energy_kcal_per_100g, protein_g_per_100g, "
     "carbs_g_per_100g, fat_g_per_100g) VALUES ('Hatalı besin', '1 adet', 10, 500, 60, 30, 20)", "23514"),
    ("DB-19", "Aynı dış besin aynı diyetisyene iki kez aktarılamaz",
     f"INSERT INTO food_items (dietitian_id, name, portion_name, portion_grams, energy_kcal_per_100g, "
     f"protein_g_per_100g, carbs_g_per_100g, fat_g_per_100g, source_name, external_id, source_updated_at) VALUES "
     f"('{I['dA']}', 'Oats', '1 cup', 80, 379, 13.2, 67.7, 6.5, 'USDA FoodData Central', '173904', now()), "
     f"('{I['dA']}', 'Oats', '1 cup', 80, 379, 13.2, 67.7, 6.5, 'USDA FoodData Central', '173904', now())",
     "23505"),
    ("DB-20", "Aynı gün için ikinci ölçüm girilemez",
     f"INSERT INTO measurements (client_id, measured_on, weight_kg, height_cm, waist_cm) VALUES "
     f"('{I['cA']}', '2026-11-01', 72.4, 165, 84), ('{I['cA']}', '2026-11-01', 72.0, 165, 83)", "23505"),
    ("DB-21", "Mantıksız kilo (500 kg) girilemez",
     f"INSERT INTO measurements (client_id, measured_on, weight_kg, height_cm, waist_cm) "
     f"VALUES ('{I['cA']}', '2026-11-02', 500, 165, 84)", "23514"),
    ("DB-22", "Aynı öğün aynı gün iki kez kaydedilemez",
     f"INSERT INTO meal_logs (client_id, meal_id, log_date, status) VALUES "
     f"('{I['cA']}', '{I['pMeal1']}', '2026-11-10', 'Eaten'), ('{I['cA']}', '{I['pMeal1']}', '2026-11-10', 'Partial')",
     "23505"),
    ("DB-23", "5 MB'tan büyük fotoğraf kaydedilemez",
     f"INSERT INTO meal_logs (client_id, meal_id, log_date, status, photo_key, photo_content_type, photo_size_bytes) "
     f"VALUES ('{I['cA']}', '{I['pMeal1']}', '2026-11-10', 'Eaten', 'meal-photos/x1', 'image/jpeg', 6000000)", "23514"),
    ("DB-24", "Yalnızca JPEG/PNG kabul edilir",
     f"INSERT INTO meal_logs (client_id, meal_id, log_date, status, photo_key, photo_content_type, photo_size_bytes) "
     f"VALUES ('{I['cA']}', '{I['pMeal1']}', '2026-11-10', 'Eaten', 'meal-photos/x2', 'image/gif', 1000)", "23514"),
    ("DB-25", "Fotoğraf bilgileri eksik (yarım) tutulamaz",
     f"INSERT INTO meal_logs (client_id, meal_id, log_date, status, photo_key) "
     f"VALUES ('{I['cA']}', '{I['pMeal1']}', '2026-11-10', 'Eaten', 'meal-photos/x3')", "23514"),
    ("DB-26", "Çift randevu: aynı diyetisyene kesişen ikinci randevu açılamaz",
     appt("dA", "cA", "2026-12-01 10:15+03", "2026-12-01 10:45+03"), "23P01"),
    ("DB-27", "Arka arkaya randevu (10:30 biter, 10:30 başlar) serbest",
     appt("dA", "cA", "2026-12-01 10:30+03", "2026-12-01 11:00+03"), "ok"),
    ("DB-28", "Farklı diyetisyenlerin aynı saatteki randevuları çakışma sayılmaz",
     appt("dB", "cB", "2026-12-01 10:00+03", "2026-12-01 10:30+03"), "ok"),
    ("DB-29", "İptal edilen randevunun saati yeniden alınabilir",
     f"UPDATE appointments SET status = 'Cancelled' WHERE id = '{I['appt1']}'; "
     + appt("dA", "cA", "2026-12-01 10:00+03", "2026-12-01 10:30+03"), "ok"),
    ("DB-30", "Aynı hatırlatma (dedup_key) iki kez üretilemez",
     f"INSERT INTO notifications (user_id, type, title, body, dedup_key) VALUES "
     f"('{I['uCA']}', 'AppointmentReminder', 'Randevu', 'Yarın randevun var', 'appt:{I['appt1']}:24h'), "
     f"('{I['uCA']}', 'AppointmentReminder', 'Randevu', 'Yarın randevun var', 'appt:{I['appt1']}:24h')", "23505"),
    ("DB-31", "Çalışma saati bitişi başlangıçtan önce olamaz",
     f"INSERT INTO working_hours (dietitian_id, weekday, start_time, end_time) VALUES ('{I['dA']}', 1, '17:00', '09:00')",
     "23514"),
]

failures = 0
print(f"{'Kod':6} {'Beklenen':8} {'Gelen':8} Sonuç  Açıklama")
for code, desc, sql, expected in TESTS:
    got = run(sql)
    ok = got == expected
    failures += 0 if ok else 1
    print(f"{code:6} {expected:8} {got:8} {'GEÇTİ' if ok else 'KALDI'}  {desc}")


# --- DB-32: eşzamanlı iki istek aynı slotu almaya çalışır --------------------------
def concurrent_double_booking():
    barrier = threading.Barrier(2)
    results = []

    def worker():
        with psycopg.connect(CONN) as c:
            barrier.wait()
            try:
                c.execute(appt("dA", "cA", "2026-12-03 14:00+03", "2026-12-03 14:30+03"))
                c.commit()
                results.append("ok")
            except psycopg.Error as e:
                c.rollback()
                results.append(e.sqlstate)

    threads = [threading.Thread(target=worker) for _ in range(2)]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    with psycopg.connect(CONN, autocommit=True) as c:  # testin bıraktığı kaydı temizle
        c.execute("DELETE FROM appointments WHERE starts_at = '2026-12-03 14:00+03'")
    return sorted(results)


got = concurrent_double_booking()
ok = got == ["23P01", "ok"]
failures += 0 if ok else 1
print(f"{'DB-32':6} {'ok+23P01':8} {'+'.join(got):8} {'GEÇTİ' if ok else 'KALDI'}  "
      f"Eşzamanlı iki talepten yalnız biri kaydedilir")

# --- DB-33: danışan silinince verileri zincirleme silinir (KVKK silme) --------------
with psycopg.connect(CONN) as c:
    c.execute(f"INSERT INTO measurements (client_id, measured_on, weight_kg, height_cm, waist_cm) "
              f"VALUES ('{I['cA']}', '2026-10-01', 74, 165, 86)")
    c.execute(f"INSERT INTO meal_logs (client_id, meal_id, log_date, status) "
              f"VALUES ('{I['cA']}', '{I['pMeal1']}', '2026-11-09', 'Eaten')")
    c.execute(f"DELETE FROM users WHERE id = '{I['uCA']}'")   # danışan hesabı silinir
    left = c.execute(f"""SELECT (SELECT count(*) FROM clients WHERE id = '{I['cA']}')
                              + (SELECT count(*) FROM measurements WHERE client_id = '{I['cA']}')
                              + (SELECT count(*) FROM nutrition_plans WHERE client_id = '{I['cA']}')
                              + (SELECT count(*) FROM meal_logs WHERE client_id = '{I['cA']}')
                              + (SELECT count(*) FROM appointments WHERE client_id = '{I['cA']}')""").fetchone()[0]
    c.rollback()
ok = left == 0
failures += 0 if ok else 1
print(f"{'DB-33':6} {'0 kayıt':8} {str(left) + ' kayıt':8} {'GEÇTİ' if ok else 'KALDI'}  "
      f"Danışan silinince ölçüm/plan/günlük/randevu kayıtları da silinir")

total = len(TESTS) + 2
print(f"\nToplam {total} test, {total - failures} geçti, {failures} kaldı.")
sys.exit(1 if failures else 0)
