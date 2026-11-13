namespace Ddts.Application.Abstractions.Email;

/// <summary>
/// Giden port (Şekil 4.4): e-posta gönderir. Uygulaması SmtpEmailAdapter (geliştirmede Mailpit).
/// Bilerek küçük tutuldu: yalnızca iki e-posta türü var.
/// </summary>
public interface IEmailSender
{
    /// <summary>Danışan daveti: bağlantıdan parolasını belirler (FR-B1, FR-A3).</summary>
    Task SendInvitationAsync(string to, string link, CancellationToken cancellationToken = default);

    /// <summary>Parola sıfırlama bağlantısı, 30 dakika geçerli (FR-A3).</summary>
    Task SendPasswordResetAsync(string to, string link, CancellationToken cancellationToken = default);
}
