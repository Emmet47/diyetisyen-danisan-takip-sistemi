using Ddts.Application.Abstractions.Security;
using Ddts.Domain.Users;
using Ddts.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ddts.Infrastructure.Security;

/// <summary>
/// ITokenService'in JWT adapter'ı (Şekil 4.4, ADR-0006). Token'a kullanıcı kimliği, rol ve
/// diyetisyen/danışan kimliği yazılır; servisler tenant bilgisini buradan alır (Şekil 5.9).
/// </summary>
public sealed class JwtTokenService(DdtsDbContext db, IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    public string CreateToken(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var claims = new Dictionary<string, object>
        {
            [DdtsClaims.Subject] = user.Id.ToString(),
            [DdtsClaims.Role] = user.Role.ToString(),
        };

        // Diyetisyen ve danışan kimliği kayıttan okunur (kullanıcı kaydedildikten sonra çağrılır).
        if (user.Role == UserRole.Dietitian)
        {
            var dietitianId = db.Dietitians.Where(d => d.UserId == user.Id).Select(d => d.Id).Single();
            claims[DdtsClaims.DietitianId] = dietitianId.ToString();
        }
        else if (user.Role == UserRole.Client)
        {
            var client = db.Clients.Where(c => c.UserId == user.Id).Select(c => new { c.Id, c.DietitianId }).Single();
            claims[DdtsClaims.ClientId] = client.Id.ToString();
            claims[DdtsClaims.DietitianId] = client.DietitianId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(AuthTokenDefaults.Lifetime),
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
