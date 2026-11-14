namespace Ddts.Api.Security;

/// <summary>Hız sınırı (rate limit) politikalarının adları. Tanımları Program.cs'te.</summary>
public static class RateLimitPolicies
{
    /// <summary>Giriş ucu: IP başına dakikada en fazla 5 deneme (NFR-G2).</summary>
    public const string Login = "login";
}
