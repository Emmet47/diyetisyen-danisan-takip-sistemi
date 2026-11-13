using Microsoft.EntityFrameworkCore.Migrations;

namespace Ddts.Infrastructure.Persistence.Migrations;

/// <summary>
/// EF Core modelinden üretilemeyen iki kısıt. SQL'leri docs/db/schema.sql'dekiyle aynı.
/// İlk migration'da (IlkSema) Up() sonunda Create, Down() başında Drop çağrılır.
///   1) ex_appointments_no_overlap: aynı diyetisyenin aktif (Talep Edildi / Onaylandı) randevuları
///      zaman olarak kesişemez; aynı anda gelen iki istekte de yalnızca biri kaydedilir (ADR-0002, FR-I4).
///   2) ux_meal_logs_photo_key: fotoğraf anahtarı tekil. EF Core 10, değer nesnesi (PhotoInfo)
///      içindeki bir alana indeks tanımlamayı desteklemiyor.
/// </summary>
internal static class ManualSqlConstraints
{
    public static void Create(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE appointments ADD CONSTRAINT ex_appointments_no_overlap EXCLUDE USING gist (
                dietitian_id WITH =,
                tstzrange(starts_at, ends_at, '[)') WITH &&
            ) WHERE (status IN ('Requested', 'Confirmed'));
            """);

        migrationBuilder.Sql("CREATE UNIQUE INDEX ux_meal_logs_photo_key ON meal_logs (photo_key);");
    }

    public static void Drop(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_meal_logs_photo_key;");
        migrationBuilder.Sql("ALTER TABLE appointments DROP CONSTRAINT IF EXISTS ex_appointments_no_overlap;");
    }
}
