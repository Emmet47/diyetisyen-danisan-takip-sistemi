namespace Ddts.Domain.Common;

/// <summary>
/// Varlıkların kurucularında tekrar eden girdi kontrolleri. Sınırlar docs/db/schema.sql'deki
/// sütun uzunlukları ve CHECK kısıtlarıyla aynı: hatalı veri veritabanına gitmeden yakalanır.
/// </summary>
internal static class Guard
{
    /// <summary>Boş olmayan, en fazla <paramref name="maxLength"/> karakterlik metin (baş/son boşluk silinir).</summary>
    public static string Required(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{fieldName} boş olamaz.");
        }

        return MaxLength(trimmed, maxLength, fieldName);
    }

    /// <summary>İsteğe bağlı metin: boşsa null olur, doluysa uzunluğu kontrol edilir.</summary>
    public static string? Optional(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : MaxLength(trimmed, maxLength, fieldName);
    }

    public static decimal InRange(decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"{fieldName} {min} ile {max} arasında olmalı.");
        }

        return value;
    }

    public static decimal? InRange(decimal? value, decimal min, decimal max, string fieldName) =>
        value is null ? null : InRange(value.Value, min, max, fieldName);

    /// <summary>
    /// timestamptz sütunlarına yalnızca UTC zaman yazılır (Npgsql başka türü kabul etmez).
    /// </summary>
    public static DateTime Utc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{fieldName} UTC olmalı (DateTimeKind.Utc).", fieldName);
        }

        return value;
    }

    private static string MaxLength(string value, int maxLength, string fieldName)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{fieldName} en fazla {maxLength} karakter olabilir.");
        }

        return value;
    }
}
