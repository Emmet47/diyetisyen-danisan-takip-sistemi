using Ddts.Domain.Appointments;

namespace Ddts.Application.Appointments;

/// <summary>Bir günün çalışma aralığı (FR-I1). Gün: 1 = Pazartesi … 7 = Pazar; saatler Türkiye saati.</summary>
public sealed record WorkingHourDto(int Weekday, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>30 dakikalık boş slot (UTC).</summary>
public sealed record SlotDto(DateTime StartsAt, DateTime EndsAt);

/// <summary>Randevu. Zamanlar UTC; ClientName yalnızca diyetisyenin takviminde dolu.</summary>
public sealed record AppointmentDto(
    Guid Id,
    Guid ClientId,
    string? ClientName,
    DateTime StartsAt,
    DateTime EndsAt,
    AppointmentStatus Status,
    string? Note);
