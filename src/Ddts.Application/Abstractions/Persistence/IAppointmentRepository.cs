using Ddts.Domain.Appointments;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Randevular (Şekil 4.10, I modülü). Diyetisyen verisi okuyan metotlar dietitianId alır.</summary>
public interface IAppointmentRepository
{
    Task<Appointment?> GetAsync(Guid dietitianId, Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>O günü kesen aktif (Talep Edildi / Onaylandı) randevular; boş slot hesabı için.</summary>
    Task<IReadOnlyList<Appointment>> ListActiveAsync(Guid dietitianId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyenin takvimi: [from, to) aralığında başlayan randevular, saate göre sıralı.</summary>
    Task<IReadOnlyList<Appointment>> ListAsync(Guid dietitianId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Danışanın randevuları, en yenisi önce.</summary>
    Task<IReadOnlyList<Appointment>> ListForClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>(from, to] aralığında başlayacak onaylı randevular (24 saat önce hatırlatma, FR-J1).</summary>
    Task<IReadOnlyList<Appointment>> ListConfirmedStartingAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    void Add(Appointment appointment);
}
