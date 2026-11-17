using Ddts.Domain.Dietitians;

namespace Ddts.Domain.Appointments;

/// <summary>
/// Domain servisi (Şekil 4.1, FR-I1, BR-08): diyetisyenin o günkü çalışma aralığını 30 dakikalık slotlara böler,
/// aktif (Talep Edildi / Onaylandı) randevularla çakışan slotları çıkarır.
/// Çalışma saatleri Türkiye saatiyle girilir; slotlar veritabanındaki gibi UTC döner.
/// Geçmişte kalan slotları, saati bilen uygulama servisi (AppointmentService) ayıklar.
/// </summary>
public sealed class AvailabilityService
{
    private static readonly TimeSpan SlotLength = TimeSpan.FromMinutes(30);

    // Türkiye saati UTC+3 (2016'dan beri yaz saati uygulaması yok).
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    public IReadOnlyList<TimeSlot> GetFreeSlots(IEnumerable<WorkingHour> hours, DateOnly date, IEnumerable<Appointment> booked)
    {
        // Gün numarası: 1 = Pazartesi ... 7 = Pazar
        var weekday = ((int)date.DayOfWeek + 6) % 7 + 1;
        var workingHour = hours.FirstOrDefault(h => h.Weekday == weekday);
        if (workingHour is null)
        {
            return [];
        }

        var active = booked
            .Where(a => a.Status is AppointmentStatus.Requested or AppointmentStatus.Confirmed)
            .Select(a => new TimeSlot(a.StartsAt, a.EndsAt))
            .ToList();

        var slots = new List<TimeSlot>();
        var start = date.ToDateTime(workingHour.StartTime);
        var end = date.ToDateTime(workingHour.EndTime);

        for (var slotStart = start; slotStart + SlotLength <= end; slotStart += SlotLength)
        {
            var slot = new TimeSlot(ToUtc(slotStart), ToUtc(slotStart + SlotLength));
            if (!active.Any(a => a.Overlaps(slot)))
            {
                slots.Add(slot);
            }
        }

        return slots;
    }

    private static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local - LocalOffset, DateTimeKind.Utc);
}
