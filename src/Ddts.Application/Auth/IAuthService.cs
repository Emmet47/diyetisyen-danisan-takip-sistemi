namespace Ddts.Application.Auth;

/// <summary>Gelen port: kayıt, giriş, parola sıfırlama (A modülü, UC-01–UC-03).</summary>
public interface IAuthService
{
    /// <summary>Diyetisyen hesabı açar, seçilen paketin ilk ayını (simülasyonla) başlatır ve giriş token'ı döner.</summary>
    Task<LoginResult> RegisterDietitianAsync(RegisterDietitianCommand command, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    /// <summary>E-posta kayıtlıysa 30 dakika geçerli sıfırlama bağlantısı gönderir; kayıtlı olmasa da hata vermez.</summary>
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Bağlantıdaki token ile yeni parola belirler. Danışan daveti de bununla tamamlanır.</summary>
    Task ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default);
}
