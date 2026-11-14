using System.ComponentModel.DataAnnotations;
using Ddts.Api.Security;
using Ddts.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ddts.Api.Controllers;

/// <summary>Kayıt, giriş ve parola sıfırlama (A modülü). Bu uç noktalar token istemez.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Diyetisyen hesabı açar ve seçilen paketi başlatır (FR-A1).</summary>
    /// <remarks>Ödeme simülasyonla yapılır. Yanıt girişle aynıdır; web paneli kayıttan sonra doğrudan oturum açabilir.</remarks>
    /// <response code="201">Hesap açıldı, token döndü.</response>
    /// <response code="400">Eksik/hatalı alan ya da paket satışta değil.</response>
    /// <response code="409">Bu e-posta ile kayıtlı hesap var.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResult>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterDietitianAsync(
            new RegisterDietitianCommand(request.FullName, request.Email, request.Password, request.Phone, request.SubscriptionPlanId),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>E-posta ve parolayla giriş; 60 dakika geçerli JWT döner (FR-A2).</summary>
    /// <remarks>IP başına dakikada en fazla 5 deneme; 6. denemede 429 (NFR-G2).</remarks>
    /// <response code="200">Giriş başarılı.</response>
    /// <response code="401">E-posta ya da parola hatalı, ya da hesap pasif.</response>
    /// <response code="429">Çok fazla deneme.</response>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResult>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(new LoginCommand(request.Email, request.Password), cancellationToken));

    /// <summary>"Parolamı unuttum": e-posta kayıtlıysa 30 dakika geçerli sıfırlama bağlantısı gönderir (FR-A3).</summary>
    /// <remarks>E-posta kayıtlı olsun olmasın hep 202 döner. Geliştirmede e-posta Mailpit'te: http://localhost:8025</remarks>
    /// <response code="202">İstek alındı.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Accepted();
    }

    /// <summary>Bağlantıdaki token ile yeni parola belirler; danışan daveti de bununla tamamlanır (FR-A3).</summary>
    /// <response code="204">Parola değişti; artık yeni parolayla giriş yapılabilir.</response>
    /// <response code="400">Token geçersiz, kullanılmış ya da süresi dolmuş; ya da parola kurala uymuyor.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);
        return NoContent();
    }
}

/// <summary>Diyetisyen kaydı.</summary>
/// <param name="FullName">Ad soyad.</param>
/// <param name="Email">Giriş e-postası.</param>
/// <param name="Password">En az 8 karakter.</param>
/// <param name="Phone">İsteğe bağlı telefon.</param>
/// <param name="SubscriptionPlanId">GET /api/subscription-plans listesinden seçilen paket.</param>
public sealed record RegisterRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(AuthService.MaxPasswordLength, MinimumLength = AuthService.MinPasswordLength)] string Password,
    [StringLength(20)] string? Phone,
    [Required] Guid SubscriptionPlanId);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress] string Email);

/// <param name="Token">E-postadaki bağlantının "token" değeri.</param>
/// <param name="NewPassword">En az 8 karakter.</param>
public sealed record ResetPasswordRequest(
    [Required] string Token,
    [Required, StringLength(AuthService.MaxPasswordLength, MinimumLength = AuthService.MinPasswordLength)] string NewPassword);
