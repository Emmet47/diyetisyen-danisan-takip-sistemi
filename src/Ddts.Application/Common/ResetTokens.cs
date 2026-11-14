using System.Security.Cryptography;
using System.Text;

namespace Ddts.Application.Common;

/// <summary>
/// Parola sıfırlama ve danışan davet bağlantısının token'ı (FR-A3: ikisi aynı mekanizma).
/// Bağlantıdaki token rastgeledir; veritabanına yalnızca SHA-256 hash'i yazılır.
/// </summary>
public static class ResetTokens
{
    /// <summary>Bağlantı 30 dakika geçerli (FR-A3).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    /// <summary>32 rastgele bayt, onaltılık yazımla 64 karakter.</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Token'ın veritabanındaki hali (SHA-256, onaltılık).</summary>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
