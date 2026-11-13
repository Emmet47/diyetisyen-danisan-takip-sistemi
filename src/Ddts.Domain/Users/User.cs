using System.Security.Cryptography;
using System.Text;
using Ddts.Domain.Common;

namespace Ddts.Domain.Users;

/// <summary>
/// Giriş hesabı (Şekil 4.1). Diyetisyen, danışan ve yönetici aynı tabloda, rolüyle ayrılır.
/// Davet edilen danışanın parolası, daveti kabul edene kadar boştur (PasswordHash = null).
/// </summary>
public sealed class User
{
    // EF Core veritabanından okurken bu kurucuyu kullanır.
    private User()
    {
    }

    public User(string email, UserRole role)
    {
        Id = Guid.CreateVersion7();
        Email = NormalizeEmail(email);
        Role = role;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    /// <summary>Hep küçük harfle saklanır; "Ayse@..." ile "ayse@..." aynı hesaptır (users tablosunda CHECK var).</summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// PBKDF2 ile hash'lenmiş parola. Diyagramdaki "-" işareti: dışarıdan değiştirilemez,
    /// yalnızca SetPassword ile değişir. Giriş sırasında doğrulama için okunur.
    /// </summary>
    public string? PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Danışan uygulamasının FCM cihaz anahtarı (yalnızca danışan; uygulama açılınca güncellenir).</summary>
    public string? PushToken { get; set; }

    // Parola sıfırlama / davet bağlantısının hash'i ve son geçerlilik anı.
    // Diyagramda özel (-) alanlar; EF Core bunları da veritabanına yazar.
    private string? ResetTokenHash { get; set; }

    private DateTime? ResetTokenExpiresAt { get; set; }

    /// <summary>Yeni parolayı yazar; kullanılmış sıfırlama bağlantısı da geçersiz olur.</summary>
    public void SetPassword(string hash)
    {
        PasswordHash = Guard.Required(hash, 200, "Parola hash'i");
        ResetTokenHash = null;
        ResetTokenExpiresAt = null;
    }

    /// <summary>Parola sıfırlama ya da danışan daveti başlatır. Bağlantıdaki token'ın yalnızca hash'i saklanır.</summary>
    public void StartPasswordReset(string tokenHash, DateTime expiresAt)
    {
        ResetTokenHash = Guard.Required(tokenHash, 100, "Sıfırlama token hash'i");
        ResetTokenExpiresAt = Guard.Utc(expiresAt, nameof(expiresAt));
    }

    public bool IsResetTokenValid(string tokenHash, DateTime now)
    {
        if (ResetTokenHash is null || ResetTokenExpiresAt is null || now >= ResetTokenExpiresAt)
        {
            return false;
        }

        // Sabit sürede karşılaştırma: yanıt süresinden token tahmin edilemesin.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(ResetTokenHash),
            Encoding.UTF8.GetBytes(tokenHash));
    }

    /// <summary>Hesabı kapatır; pasif kullanıcı giriş yapamaz (her istekte kontrol edilir, ADR-0006).</summary>
    public void Deactivate() => IsActive = false;

    private static string NormalizeEmail(string email)
    {
        var normalized = Guard.Required(email, 254, "E-posta").ToLowerInvariant();

        if (!normalized.Contains('@'))
        {
            throw new DomainException("Geçerli bir e-posta adresi girin.");
        }

        return normalized;
    }
}
