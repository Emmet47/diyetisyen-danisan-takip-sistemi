-- =====================================================================
-- Diyetisyen Danışan Takip Sistemi (DDTS) - veritabanı şeması (sade sürüm)
-- Hedef: PostgreSQL 18 (16 ve üstünde de çalışır)
-- 17 tablo: hocanın verdiği 13 varlık + 4 yardımcı tablo
--   yardımcılar: users (giriş), working_hours (uygun saatler),
--                meal_items (öğün-besin satırı), client_food_restrictions (alerji/kısıt)
-- Not: Tablolar EF Core migration ile de üretilebilir; bu dosya ER diyagramıyla
--      birebir aynı olan referans şemadır.
-- =====================================================================

-- EXCLUDE kısıtında uuid için "=" karşılaştırması btree_gist eklentisini ister
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ---------------------------------------------------------------------
-- A. Kimlik ve yetkilendirme
-- ---------------------------------------------------------------------
CREATE TABLE users (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email                   varchar(254) NOT NULL UNIQUE,
    password_hash           varchar(200),            -- danışan davetini kabul edene kadar NULL
    role                    varchar(10)  NOT NULL CHECK (role IN ('Admin', 'Dietitian', 'Client')),
    is_active               boolean      NOT NULL DEFAULT true,
    reset_token_hash        varchar(100),            -- parola sıfırlama / davet bağlantısı (hash'li)
    reset_token_expires_at  timestamptz,
    push_token              varchar(300),            -- FCM cihaz anahtarı (sadece danışan)
    created_at              timestamptz  NOT NULL DEFAULT now(),
    CHECK (email = lower(email)),                    -- e-posta küçük harfle saklanır -> büyük/küçük harf farkı tekilliği bozmaz
    CHECK ((reset_token_hash IS NULL) = (reset_token_expires_at IS NULL))
);

CREATE TABLE dietitians (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     uuid NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name   varchar(100) NOT NULL,
    phone       varchar(20),
    created_at  timestamptz NOT NULL DEFAULT now()
);

-- I. Diyetisyenin haftalık çalışma saatleri (günde tek aralık; 1 = Pazartesi ... 7 = Pazar)
CREATE TABLE working_hours (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id  uuid NOT NULL REFERENCES dietitians(id) ON DELETE CASCADE,
    weekday       smallint NOT NULL CHECK (weekday BETWEEN 1 AND 7),
    start_time    time NOT NULL,
    end_time      time NOT NULL,
    CHECK (start_time < end_time),
    UNIQUE (dietitian_id, weekday)
);

-- ---------------------------------------------------------------------
-- L. Abonelik
-- ---------------------------------------------------------------------
CREATE TABLE subscription_plans (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name           varchar(50)  NOT NULL UNIQUE,
    max_clients    int          NOT NULL CHECK (max_clients > 0),
    monthly_price  numeric(8,2) NOT NULL CHECK (monthly_price >= 0),
    is_active      boolean      NOT NULL DEFAULT true    -- yönetici paketi satıştan kaldırabilir
);

CREATE TABLE subscriptions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id          uuid NOT NULL REFERENCES dietitians(id) ON DELETE CASCADE,
    subscription_plan_id  uuid NOT NULL REFERENCES subscription_plans(id),
    status                varchar(10) NOT NULL CHECK (status IN ('Active', 'Expired', 'Cancelled')),
    start_date            date NOT NULL,
    end_date              date NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CHECK (start_date < end_date)
);
-- Bir diyetisyenin aynı anda yalnızca bir aktif aboneliği olabilir
CREATE UNIQUE INDEX ux_subscriptions_one_active ON subscriptions (dietitian_id) WHERE status = 'Active';

-- ---------------------------------------------------------------------
-- B. Danışan profili
-- ---------------------------------------------------------------------
CREATE TABLE clients (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id           uuid NOT NULL REFERENCES dietitians(id),   -- tenant: danışan tek diyetisyene bağlı
    user_id                uuid NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    full_name              varchar(100) NOT NULL,
    phone                  varchar(20),
    birth_date             date NOT NULL,
    sex                    varchar(6)  NOT NULL CHECK (sex IN ('Female', 'Male')),
    goal_type              varchar(10) NOT NULL CHECK (goal_type IN ('LoseWeight', 'Maintain', 'GainWeight')),
    target_weight_kg       numeric(5,1) CHECK (target_weight_kg BETWEEN 20 AND 350),
    preferences            varchar(500),     -- beslenme tercihleri (serbest metin)
    allergy_notes          varchar(500),     -- alerji notu (serbest metin)
    is_active              boolean NOT NULL DEFAULT true,
    kvkk_consent_at        timestamptz NOT NULL,   -- açık rıza alındığı an (eklerken zorunlu)
    notify_appointments    boolean NOT NULL DEFAULT true,   -- J. bildirim izinleri
    notify_meal_reminders  boolean NOT NULL DEFAULT true,
    notify_measurements    boolean NOT NULL DEFAULT true,
    notify_plans           boolean NOT NULL DEFAULT true,
    created_at             timestamptz NOT NULL DEFAULT now(),
    UNIQUE (id, dietitian_id)      -- plan ve randevu tablolarındaki bileşik FK için
);
CREATE INDEX ix_clients_dietitian ON clients (dietitian_id);

-- ---------------------------------------------------------------------
-- D. Besin veritabanı (yerel kayıtlar)
-- ---------------------------------------------------------------------
CREATE TABLE food_items (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id          uuid REFERENCES dietitians(id) ON DELETE CASCADE,  -- NULL = herkese açık ortak kayıt
    name                  varchar(150) NOT NULL,
    portion_name          varchar(50)  NOT NULL,                             -- "1 dilim", "1 su bardağı"
    portion_grams         numeric(6,1) NOT NULL CHECK (portion_grams > 0),
    energy_kcal_per_100g  numeric(6,1) NOT NULL CHECK (energy_kcal_per_100g BETWEEN 0 AND 900),
    protein_g_per_100g    numeric(5,1) NOT NULL CHECK (protein_g_per_100g BETWEEN 0 AND 100),
    carbs_g_per_100g      numeric(5,1) NOT NULL CHECK (carbs_g_per_100g BETWEEN 0 AND 100),
    fat_g_per_100g        numeric(5,1) NOT NULL CHECK (fat_g_per_100g BETWEEN 0 AND 100),
    source_name           varchar(50)  NOT NULL DEFAULT 'Yerel',            -- veri kaynağı
    external_id           varchar(50),                                      -- dış kaynaktaki kimlik (ör. USDA fdcId)
    source_updated_at     timestamptz,                                      -- güncellik: dış kaynaktan çekildiği an
    created_at            timestamptz NOT NULL DEFAULT now(),
    CHECK (protein_g_per_100g + carbs_g_per_100g + fat_g_per_100g <= 100),
    CHECK ((external_id IS NULL) = (source_updated_at IS NULL))
);
-- Aynı dış besin aynı diyetisyen tarafından iki kez içe aktarılamaz
CREATE UNIQUE INDEX ux_food_items_external ON food_items (dietitian_id, source_name, external_id)
    WHERE external_id IS NOT NULL;

-- B. Danışanın alerji/kısıt listesi: Allergy -> plana eklenemez, Dislike -> uyarı verilir
CREATE TABLE client_food_restrictions (
    client_id     uuid NOT NULL REFERENCES clients(id) ON DELETE CASCADE,
    food_item_id  uuid NOT NULL REFERENCES food_items(id) ON DELETE CASCADE,
    type          varchar(10) NOT NULL CHECK (type IN ('Allergy', 'Dislike')),
    PRIMARY KEY (client_id, food_item_id)
);

-- ---------------------------------------------------------------------
-- C. Ölçümler
-- ---------------------------------------------------------------------
CREATE TABLE measurements (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id         uuid NOT NULL REFERENCES clients(id) ON DELETE CASCADE,
    measured_on       date NOT NULL,
    weight_kg         numeric(5,1) NOT NULL CHECK (weight_kg BETWEEN 20 AND 350),
    height_cm         numeric(4,1) NOT NULL CHECK (height_cm BETWEEN 100 AND 250),
    waist_cm          numeric(4,1) NOT NULL CHECK (waist_cm BETWEEN 30 AND 250),
    hip_cm            numeric(4,1) CHECK (hip_cm BETWEEN 30 AND 250),             -- isteğe bağlı
    body_fat_percent  numeric(4,1) CHECK (body_fat_percent BETWEEN 2 AND 75),     -- isteğe bağlı
    note              varchar(300),
    created_at        timestamptz NOT NULL DEFAULT now(),
    UNIQUE (client_id, measured_on)    -- aynı gün için tek ölçüm
);

-- ---------------------------------------------------------------------
-- E / F. Plan şablonu, beslenme planı, öğün, öğün satırı
-- ---------------------------------------------------------------------
CREATE TABLE plan_templates (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id  uuid NOT NULL REFERENCES dietitians(id) ON DELETE CASCADE,
    name          varchar(100) NOT NULL,
    description   varchar(500),
    plan_type     varchar(6) NOT NULL CHECK (plan_type IN ('Daily', 'Weekly')),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE nutrition_plans (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id        uuid NOT NULL REFERENCES dietitians(id),
    client_id           uuid NOT NULL,
    source_template_id  uuid REFERENCES plan_templates(id) ON DELETE SET NULL,  -- şablondan kopyalandıysa
    title               varchar(100) NOT NULL,
    plan_type           varchar(6)  NOT NULL CHECK (plan_type IN ('Daily', 'Weekly')),
    status              varchar(10) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Published', 'Archived')),
    target_kcal         int CHECK (target_kcal BETWEEN 800 AND 6000),
    energy_formula      varchar(30),     -- hedefin hangi formülle hesaplandığı (Strategy adı)
    created_at          timestamptz NOT NULL DEFAULT now(),
    published_at        timestamptz,
    archived_at         timestamptz,
    -- danışan ile plan aynı diyetisyene ait olmak zorunda (tenant tutarlılığı)
    FOREIGN KEY (client_id, dietitian_id) REFERENCES clients (id, dietitian_id) ON DELETE CASCADE,
    -- durum ile tarih alanları tutarlı olmalı: Taslak -> Yayımlandı -> Arşiv
    CHECK (
        (status = 'Draft'     AND published_at IS NULL     AND archived_at IS NULL) OR
        (status = 'Published' AND published_at IS NOT NULL AND archived_at IS NULL) OR
        (status = 'Archived'  AND published_at IS NOT NULL AND archived_at IS NOT NULL)
    )
);
-- Bir danışanın aynı anda tek yayımlanmış planı olur
CREATE UNIQUE INDEX ux_nutrition_plans_one_published ON nutrition_plans (client_id) WHERE status = 'Published';
CREATE INDEX ix_nutrition_plans_dietitian ON nutrition_plans (dietitian_id);

-- Öğün ya bir plana ya da bir şablona aittir (ikisi birden değil)
CREATE TABLE meals (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id      uuid REFERENCES nutrition_plans(id) ON DELETE CASCADE,
    template_id  uuid REFERENCES plan_templates(id)  ON DELETE CASCADE,
    day_no       smallint NOT NULL CHECK (day_no BETWEEN 1 AND 7),   -- günlük planda hep 1
    name         varchar(50) NOT NULL,                               -- Kahvaltı, Ara öğün, Öğle ...
    meal_time    time NOT NULL,
    CHECK (num_nonnulls(plan_id, template_id) = 1)
);
CREATE INDEX ix_meals_plan ON meals (plan_id);
CREATE INDEX ix_meals_template ON meals (template_id);

-- Öğündeki besin satırı (Composite'in yaprağı). Alternatif satır, aynı öğündeki bir ana satırı gösterir.
CREATE TABLE meal_items (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    meal_id                 uuid NOT NULL REFERENCES meals(id) ON DELETE CASCADE,
    food_item_id            uuid NOT NULL REFERENCES food_items(id) ON DELETE RESTRICT,  -- kullanılan besin silinemez
    portion_count           numeric(5,2) NOT NULL CHECK (portion_count > 0 AND portion_count <= 20),
    alternative_to_item_id  uuid,                                                        -- NULL = ana satır
    UNIQUE (id, meal_id),
    FOREIGN KEY (alternative_to_item_id, meal_id) REFERENCES meal_items (id, meal_id) ON DELETE CASCADE,
    CHECK (alternative_to_item_id IS NULL OR alternative_to_item_id <> id)
);
CREATE INDEX ix_meal_items_meal ON meal_items (meal_id);

-- ---------------------------------------------------------------------
-- G. Öğün günlüğü ve fotoğraf
-- ---------------------------------------------------------------------
CREATE TABLE meal_logs (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id           uuid NOT NULL REFERENCES clients(id) ON DELETE CASCADE,
    meal_id             uuid NOT NULL REFERENCES meals(id) ON DELETE CASCADE,
    log_date            date NOT NULL,
    status              varchar(8) NOT NULL CHECK (status IN ('Eaten', 'Partial', 'Skipped')),
    note                varchar(500),
    photo_key           varchar(200) UNIQUE,      -- nesne depolamadaki rastgele anahtar
    photo_content_type  varchar(10) CHECK (photo_content_type IN ('image/jpeg', 'image/png')),
    photo_size_bytes    int CHECK (photo_size_bytes BETWEEN 1 AND 5242880),   -- en fazla 5 MB
    logged_at           timestamptz NOT NULL DEFAULT now(),
    UNIQUE (client_id, meal_id, log_date),         -- bir öğün aynı gün bir kez kaydedilir
    CHECK (num_nonnulls(photo_key, photo_content_type, photo_size_bytes) IN (0, 3))
);
CREATE INDEX ix_meal_logs_client_date ON meal_logs (client_id, log_date);

-- ---------------------------------------------------------------------
-- I. Randevular
-- ---------------------------------------------------------------------
CREATE TABLE appointments (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    dietitian_id  uuid NOT NULL REFERENCES dietitians(id),
    client_id     uuid NOT NULL,
    starts_at     timestamptz NOT NULL,
    ends_at       timestamptz NOT NULL,
    status        varchar(10) NOT NULL CHECK (status IN ('Requested', 'Confirmed', 'Cancelled', 'Completed')),
    note          varchar(300),
    created_at    timestamptz NOT NULL DEFAULT now(),
    CHECK (starts_at < ends_at),
    FOREIGN KEY (client_id, dietitian_id) REFERENCES clients (id, dietitian_id) ON DELETE CASCADE,
    -- Çift randevu önleme: aynı diyetisyenin "Talep Edildi" ya da "Onaylandı" durumundaki
    -- iki randevusunun zaman aralığı kesişemez. Eşzamanlı isteklerde de veritabanı korur.
    CONSTRAINT ex_appointments_no_overlap EXCLUDE USING gist (
        dietitian_id WITH =,
        tstzrange(starts_at, ends_at, '[)') WITH &&
    ) WHERE (status IN ('Requested', 'Confirmed'))
);
CREATE INDEX ix_appointments_client ON appointments (client_id);

-- ---------------------------------------------------------------------
-- J. Bildirimler
-- ---------------------------------------------------------------------
CREATE TABLE notifications (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type        varchar(30) NOT NULL CHECK (type IN (
                    'AppointmentRequested', 'AppointmentConfirmed', 'AppointmentCancelled',
                    'AppointmentRescheduled', 'AppointmentReminder',
                    'PlanPublished', 'MealLogReminder', 'MeasurementReminder')),
    title       varchar(100) NOT NULL,
    body        varchar(300) NOT NULL,      -- sağlık verisi yazılmaz (ör. "Yeni planın hazır")
    is_read     boolean NOT NULL DEFAULT false,
    dedup_key   varchar(100) UNIQUE,        -- aynı hatırlatma iki kez üretilmesin (ör. appt:{id}:24h)
    created_at  timestamptz NOT NULL DEFAULT now(),
    sent_at     timestamptz                 -- push gönderildiyse zamanı
);
CREATE INDEX ix_notifications_user ON notifications (user_id, created_at DESC);

-- ---------------------------------------------------------------------
-- Denetim izi (AuditLog)
-- ---------------------------------------------------------------------
CREATE TABLE audit_logs (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id      uuid REFERENCES users(id) ON DELETE SET NULL,   -- başarısız girişte NULL olabilir
    action       varchar(50) NOT NULL,      -- LoginSucceeded, ClientUpdated, PlanPublished ...
    entity_type  varchar(50) NOT NULL,      -- Client, NutritionPlan, Appointment ...
    entity_id    uuid,
    details      jsonb,                     -- değişiklik geçmişi: {"alan": {"old": .., "new": ..}}
    ip_address   varchar(45),
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_type, entity_id, created_at);
