using Ddts.Domain.Users;

namespace Ddts.Application.Auth;

/// <summary>Diyetisyen kaydı (FR-A1): hesap bilgileri ve seçilen paket.</summary>
public sealed record RegisterDietitianCommand(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    Guid SubscriptionPlanId);

public sealed record LoginCommand(string Email, string Password);

public sealed record ResetPasswordCommand(string Token, string NewPassword);

/// <summary>Giriş yanıtı (docs/api.md 8.3): token, son geçerlilik anı (UTC) ve rol.</summary>
public sealed record LoginResult(string AccessToken, DateTime ExpiresAt, UserRole Role);
