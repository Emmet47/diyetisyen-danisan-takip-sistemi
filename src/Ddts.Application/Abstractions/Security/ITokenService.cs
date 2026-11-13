using Ddts.Domain.Users;

namespace Ddts.Application.Abstractions.Security;

/// <summary>
/// Giden port (Şekil 4.4): giriş yapan kullanıcıya JWT üretir (ADR-0006).
/// Token'da kullanıcı kimliği, rol ve diyetisyen/danışan kimliği claim olarak bulunur.
/// </summary>
public interface ITokenService
{
    string CreateToken(User user);
}

/// <summary>Token ayarları: süre hem token'a hem giriş yanıtındaki expiresAt'e yazılır.</summary>
public static class AuthTokenDefaults
{
    /// <summary>JWT 60 dakika geçerli (FR-A2), yenileme token'ı yok.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(60);
}
