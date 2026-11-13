using System.Security.Claims;
using Ddts.Application.Abstractions.Security;
using Ddts.Domain.Users;
using Ddts.Infrastructure.Security;

namespace Ddts.Api.Security;

/// <summary>
/// ICurrentUser'ın adapter'ı (Şekil 4.4): bilgileri isteğin JWT claim'lerinden okur.
/// Yalnızca [Authorize] uç noktalarında kullanılır; token yoksa istek buraya gelmeden 401 döner.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId => Guid.Parse(Required(DdtsClaims.Subject));

    public UserRole Role => Enum.Parse<UserRole>(Required(DdtsClaims.Role));

    public Guid? DietitianId => Optional(DdtsClaims.DietitianId);

    public Guid? ClientId => Optional(DdtsClaims.ClientId);

    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("Etkin bir HTTP isteği yok.");

    private string Required(string claimType) =>
        Principal.FindFirst(claimType)?.Value
        ?? throw new InvalidOperationException($"Token'da '{claimType}' bilgisi yok (giriş yapılmamış).");

    private Guid? Optional(string claimType) =>
        Guid.TryParse(Principal.FindFirst(claimType)?.Value, out var id) ? id : null;
}
