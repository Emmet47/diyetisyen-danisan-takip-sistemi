namespace Ddts.Application.Common;

/// <summary>
/// Türkiye saatiyle tarih işlemleri. Veritabanında zamanlar UTC tutulur; "bugün", öğün günü, çalışma saati ve
/// bildirim metinlerindeki saat ise kullanıcının yerel saatine (UTC+3, yaz saati yok) göredir.
/// </summary>
public static class LocalTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(3);

    public static DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc + Offset, DateTimeKind.Unspecified);

    public static DateOnly DateOf(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    public static DateOnly Today(TimeProvider timeProvider) => DateOf(timeProvider.GetUtcNow().UtcDateTime);

    /// <summary>Yerel gün başlangıcının UTC karşılığı (ör. 1 Aralık 00:00 TSİ = 30 Kasım 21:00 UTC).</summary>
    public static DateTime StartOfDayUtc(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);

    /// <summary>Gün numarası: 1 = Pazartesi ... 7 = Pazar (haftalık plan ve çalışma saatleriyle aynı).</summary>
    public static int Weekday(DateOnly date) => ((int)date.DayOfWeek + 6) % 7 + 1;

    /// <summary>Bildirim metinlerinde kullanılan biçim: 01.12.2026 10:00.</summary>
    public static string Format(DateTime utc) =>
        ToLocal(utc).ToString("dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>İstekten gelen zamanı UTC'ye çevirir; saat dilimi belirtilmemişse (sonunda Z ya da +03:00 yoksa) 400.</summary>
    public static DateTime RequireUtc(DateTime value, string fieldName) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => throw new Ddts.Domain.Common.DomainException(
            $"{fieldName} saat dilimiyle gönderilmeli (ör. 2026-12-01T07:00:00Z ya da 2026-12-01T10:00:00+03:00)."),
    };
}
