using Ddts.Domain.Common;

namespace Ddts.Domain.MealLogs;

/// <summary>
/// Danışanın planlı bir öğünü için gün kaydı (Şekil 4.2, FR-G1–G2): yedim / kısmen / yemedim,
/// isteğe bağlı not ve en fazla bir fotoğraf. Aynı öğün aynı gün için bir kez kaydedilir, sonra güncellenir.
/// </summary>
public sealed class MealLog
{
    private MealLog()
    {
    }

    public MealLog(Guid clientId, Guid mealId, DateOnly logDate, MealLogStatus status, string? note)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        MealId = mealId;
        LogDate = logDate;
        Status = status;
        Note = Guard.Optional(note, 500, "Not");
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    /// <summary>Planlanan öğün (planlanan–gerçekleşen karşılaştırması bununla yapılır, FR-G3).</summary>
    public Guid MealId { get; private set; }

    public DateOnly LogDate { get; private set; }

    public MealLogStatus Status { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Fotoğraf yoksa null.</summary>
    public PhotoInfo? Photo { get; private set; }

    /// <summary>Kaydın oluşturulduğu an; veritabanı yazar (DEFAULT now()).</summary>
    public DateTime LoggedAt { get; private set; }

    public void Update(MealLogStatus status, string? note)
    {
        Status = status;
        Note = Guard.Optional(note, 500, "Not");
    }

    /// <summary>Öğün kaydına fotoğraf ekler; kayıt başına tek fotoğraf (BR-10).</summary>
    public void AttachPhoto(PhotoInfo photo)
    {
        if (Photo is not null)
        {
            throw new DomainException("Bu öğün kaydında zaten bir fotoğraf var.");
        }

        Photo = photo;
    }
}
