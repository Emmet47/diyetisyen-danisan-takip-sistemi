using Ddts.Domain.Common;

namespace Ddts.Domain.Appointments;

/// <summary>
/// Randevu zaman aralığı (değer nesnesi, Şekil 4.3). Başlangıç dahil, bitiş hariç: [Start, End).
/// Veritabanındaki EXCLUDE kısıtı da aynı aralık tanımını kullanır: tstzrange(starts_at, ends_at, '[)').
/// Böylece 10:00–10:30 ile 10:30–11:00 çakışmaz.
/// </summary>
public sealed record TimeSlot
{
    public TimeSlot(DateTime start, DateTime end)
    {
        Guard.Utc(start, nameof(start));
        Guard.Utc(end, nameof(end));

        if (start >= end)
        {
            throw new DomainException("Randevu bitişi başlangıcından sonra olmalı.");
        }

        Start = start;
        End = end;
    }

    public DateTime Start { get; }

    public DateTime End { get; }

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;
}
