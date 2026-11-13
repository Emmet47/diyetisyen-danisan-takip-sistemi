using Ddts.Application.Abstractions.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ddts.Infrastructure.Email;

/// <summary>
/// IEmailSender'ın SMTP adapter'ı (Şekil 4.4), MailKit ile. E-postalarda sağlık verisi yok;
/// yalnızca bağlantı var.
/// </summary>
public sealed class SmtpEmailAdapter(IOptions<SmtpOptions> options) : IEmailSender
{
    public Task SendInvitationAsync(string to, string link, CancellationToken cancellationToken = default) =>
        SendAsync(
            to,
            "Diyetisyen Danışan Takip Sistemi'ne davet edildin",
            "Merhaba,\n\nDiyetisyenin seni sisteme ekledi. Parolanı belirlemek için bağlantıyı aç:\n\n" +
            $"{link}\n\nBağlantı 30 dakika geçerli ve tek kullanımlık.",
            cancellationToken);

    public Task SendPasswordResetAsync(string to, string link, CancellationToken cancellationToken = default) =>
        SendAsync(
            to,
            "Parola sıfırlama",
            "Merhaba,\n\nParolanı sıfırlamak için bağlantıyı aç:\n\n" +
            $"{link}\n\nBağlantı 30 dakika geçerli ve tek kullanımlık. Bu isteği sen yapmadıysan e-postayı yok sayabilirsin.",
            cancellationToken);

    private async Task SendAsync(string to, string subject, string text, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = text };

        using var client = new SmtpClient();
        // Auto: sunucu destekliyorsa STARTTLS ile şifreler (Mailpit'te düz bağlantı).
        await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.Auto, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.UserName))
        {
            await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
