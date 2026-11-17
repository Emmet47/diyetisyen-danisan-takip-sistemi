using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Dış API için saatlik istek sayacı (Şekil 4.9, singleton). Son bir saatteki istek sayısı kotaya ulaştıysa
/// yeni istek yapılmaz; arama yerel sonuçlarla devam eder (FR-D4).
/// </summary>
public sealed class ApiQuotaCounter(IOptions<UsdaOptions> options, TimeProvider timeProvider)
{
    private readonly Queue<DateTimeOffset> _requests = new();
    private readonly Lock _lock = new();

    public bool TryConsume()
    {
        var now = timeProvider.GetUtcNow();
        var limitPerHour = options.Value.HourlyQuota;

        lock (_lock)
        {
            while (_requests.Count > 0 && now - _requests.Peek() >= TimeSpan.FromHours(1))
            {
                _requests.Dequeue();
            }

            if (_requests.Count >= limitPerHour)
            {
                return false;
            }

            _requests.Enqueue(now);
            return true;
        }
    }
}
