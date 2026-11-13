using Ddts.Application.Common;
using Ddts.Domain.Common;
using Ddts.Domain.Subscriptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ddts.Api.Errors;

/// <summary>
/// Servislerden gelen hataları HTTP durum koduna ve ProblemDetails (RFC 9457) gövdesine çevirir
/// (docs/api.md 8.1). Tanımadığı hatayı ASP.NET Core'a bırakır: 500, ayrıntısız ProblemDetails.
/// Kullanıcıya stack trace ya da SQL ayrıntısı gösterilmez (belge 7.5).
/// </summary>
public sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = ToProblem(exception);
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblem(Exception exception) => exception switch
    {
        // Sıra önemli: SubscriptionLimitException bir DomainException olduğu için önce gelir.
        SubscriptionLimitException e => Problem(StatusCodes.Status409Conflict, "Paket limiti dolu", e.Message,
            "https://ddts.example/errors/subscription-limit"),
        ConflictException e => Problem(StatusCodes.Status409Conflict, "Çakışma", e.Message),
        NotFoundException e => Problem(StatusCodes.Status404NotFound, "Bulunamadı", e.Message),
        AuthenticationFailedException e => Problem(StatusCodes.Status401Unauthorized, "Giriş başarısız", e.Message),
        DomainException e => DomainProblem(e),

        // Veritabanı kısıtları son savunma hattı: aynı anda gelen iki istekten biri tekil kısıta ya da
        // randevu çakışma kısıtına (EXCLUDE) takılırsa 500 değil 409 döner.
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            Problem(StatusCodes.Status409Conflict, "Çakışma", "Bu kayıt az önce başka bir istekle oluşturuldu. Sayfayı yenileyip tekrar dene."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation } } =>
            Problem(StatusCodes.Status409Conflict, "Bu saat az önce doldu", "Lütfen başka bir saat seç."),

        _ => null,
    };

    private static ProblemDetails DomainProblem(DomainException exception)
    {
        var problem = Problem(StatusCodes.Status400BadRequest, "İş kuralı ihlali", exception.Message);

        // Birden çok hata varsa (ör. plan sihirbazı) hepsi "errors" alanında listelenir.
        if (exception.Errors.Count > 1)
        {
            problem.Extensions["errors"] = new Dictionary<string, IReadOnlyList<string>> { ["genel"] = exception.Errors };
        }

        return problem;
    }

    private static ProblemDetails Problem(int status, string title, string detail, string? type = null) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = type,
    };
}
