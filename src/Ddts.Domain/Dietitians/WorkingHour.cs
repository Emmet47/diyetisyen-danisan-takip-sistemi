using Ddts.Domain.Common;

namespace Ddts.Domain.Dietitians;

/// <summary>
/// Diyetisyenin bir gündeki çalışma aralığı (Şekil 4.1). Her gün için tek aralık (FR-I1).
/// Weekday: 1 = Pazartesi … 7 = Pazar. Kendi kimliği diyagramda yok; tablodaki id'yi EF Core yönetir.
/// </summary>
public sealed class WorkingHour
{
    private WorkingHour()
    {
    }

    public WorkingHour(int weekday, TimeOnly startTime, TimeOnly endTime)
    {
        if (weekday is < 1 or > 7)
        {
            throw new DomainException("Gün 1 (Pazartesi) ile 7 (Pazar) arasında olmalı.");
        }

        if (startTime >= endTime)
        {
            throw new DomainException("Çalışma saati başlangıcı bitişinden önce olmalı.");
        }

        Weekday = weekday;
        StartTime = startTime;
        EndTime = endTime;
    }

    public int Weekday { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }
}
