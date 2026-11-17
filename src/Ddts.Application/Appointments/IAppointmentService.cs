namespace Ddts.Application.Appointments;

/// <summary>Gelen port: randevu yönetimi (I modülü, FR-I1–I4, Şekil 5.7).</summary>
public interface IAppointmentService
{
    /// <summary>Diyetisyenin haftalık çalışma saatlerini baştan kaydeder (her gün için tek aralık).</summary>
    Task SetWorkingHoursAsync(IReadOnlyList<WorkingHourDto> hours, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkingHourDto>> GetWorkingHoursAsync(CancellationToken cancellationToken = default);

    /// <summary>Danışanın diyetisyeninin o günkü boş ve gelecekteki slotları (/api/me/available-slots).</summary>
    Task<IReadOnlyList<SlotDto>> GetFreeSlotsAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Danışan randevu talep eder (Talep Edildi). Slot doluysa 409 "Bu saat az önce doldu".</summary>
    Task<AppointmentDto> RequestAsync(DateTime startsAt, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyen doğrudan onaylı randevu oluşturur.</summary>
    Task<AppointmentDto> CreateConfirmedAsync(Guid clientId, DateTime startsAt, CancellationToken cancellationToken = default);

    Task<AppointmentDto> ConfirmAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyen ya da danışan iptal eder (giriş yapan kişinin rolüne göre).</summary>
    Task<AppointmentDto> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Erteleme: danışan ertelerse yeniden Talep Edildi, diyetisyen ertelerse Onaylandı (BR-09).</summary>
    Task<AppointmentDto> RescheduleAsync(Guid appointmentId, DateTime startsAt, CancellationToken cancellationToken = default);

    /// <summary>Görüşme sonrası diyetisyen "Tamamlandı" yapar (yalnızca onaylı ve saati geçmiş randevu).</summary>
    Task<AppointmentDto> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyenin takvimi: [from, to) arasında başlayan randevular (varsayılan: bugünden 30 gün).</summary>
    Task<IReadOnlyList<AppointmentDto>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapmış danışanın randevuları.</summary>
    Task<IReadOnlyList<AppointmentDto>> ListMyAsync(CancellationToken cancellationToken = default);
}
