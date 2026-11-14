using Ddts.Domain.Common;

namespace Ddts.Application.Common;

/// <summary>Sayfalı liste yanıtı (docs/api.md 8.1: ?page=1&amp;pageSize=20).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Sayfa ayarları: sayfa 1'den başlar, sayfa boyutu en fazla 100.</summary>
public static class Paging
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static void Ensure(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > MaxPageSize)
        {
            throw new DomainException($"Sayfa 1 ya da daha büyük, sayfa boyutu 1 ile {MaxPageSize} arasında olmalı.");
        }
    }
}
