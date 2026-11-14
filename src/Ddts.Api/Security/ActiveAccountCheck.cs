using Ddts.Application.Abstractions.Persistence;
using Ddts.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Ddts.Api.Security;

/// <summary>
/// Her istekte hesabın hâlâ aktif olduğunu kontrol eder (ADR-0006): pasife alınan kullanıcının
/// token'ı süresi dolmadan da çalışmaz, istek 401 alır.
/// </summary>
public static class ActiveAccountCheck
{
    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirst(DdtsClaims.Subject)?.Value, out var userId))
        {
            context.Fail("Token'da kullanıcı kimliği yok.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.GetAsync(userId, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive)
        {
            context.Fail("Hesap pasif ya da silinmiş.");
        }
    }
}
