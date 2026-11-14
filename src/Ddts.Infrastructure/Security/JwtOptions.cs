using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ddts.Infrastructure.Security;

/// <summary>
/// JWT ayarları ("Jwt" bölümü). İmza anahtarı yalnızca sunucuda durur: yerelde user-secrets,
/// Docker'da .env (NFR-G5). Token'ı üreten (JwtTokenService) ve doğrulayan (Program.cs) aynı ayarı kullanır.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 için en az 32 karakter.</summary>
    public const int MinSigningKeyLength = 32;

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ddts-api";

    public string Audience { get; set; } = "ddts-clients";

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CreateSigningKey(),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = DdtsClaims.Subject,
        RoleClaimType = DdtsClaims.Role,
    };
}
