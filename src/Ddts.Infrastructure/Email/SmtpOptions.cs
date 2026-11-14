namespace Ddts.Infrastructure.Email;

/// <summary>
/// SMTP ayarları ("Smtp" bölümü). Geliştirmede Mailpit (localhost:1025, kullanıcı adı yok);
/// gelen kutusu http://localhost:8025.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string From { get; set; } = "DDTS <no-reply@ddts.local>";

    /// <summary>Boşsa kimlik doğrulama yapılmaz (Mailpit).</summary>
    public string? UserName { get; set; }

    public string? Password { get; set; }
}
