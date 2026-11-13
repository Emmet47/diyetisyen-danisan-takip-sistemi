using Ddts.Domain.Common;

namespace Ddts.Domain.Dietitians;

/// <summary>
/// Diyetisyen (Şekil 4.1). Sistemdeki her danışan, plan, şablon ve randevu bir diyetisyene aittir:
/// veri izolasyonu (tenant) bu kimlikle yapılır (NFR-G1).
/// </summary>
public sealed class Dietitian
{
    private readonly List<WorkingHour> _workingHours = [];

    private Dietitian()
    {
    }

    public Dietitian(Guid userId, string fullName, string? phone)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        FullName = Guard.Required(fullName, 100, "Ad soyad");
        Phone = Guard.Optional(phone, 20, "Telefon");
    }

    public Guid Id { get; private set; }

    /// <summary>Giriş hesabı (users tablosu).</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public IReadOnlyCollection<WorkingHour> WorkingHours => _workingHours.AsReadOnly();

    /// <summary>
    /// Haftalık çalışma saatlerini toptan değiştirir (en fazla 7 gün, her güne tek aralık).
    /// Listede olmayan günlerde randevu alınamaz.
    /// </summary>
    public void SetWorkingHours(IEnumerable<WorkingHour> hours)
    {
        var list = hours.ToList();

        if (list.Select(h => h.Weekday).Distinct().Count() != list.Count)
        {
            throw new DomainException("Her gün için yalnızca bir çalışma aralığı girilebilir.");
        }

        _workingHours.Clear();
        _workingHours.AddRange(list);
    }
}
