namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Dış API için saatlik istek sayacı (Şekil 4.9). Her dış kaynağın kendi sayacı vardır; Program.cs'te
/// anahtarla tekil (singleton) kaydedilir ("usda", "platerank"). Son bir saatteki istek sayısı kotaya
/// ulaştıysa yeni istek yapılmaz; arama yerel sonuçlarla devam eder (FR-D4).
/// </summary>
public sealed class ApiQuotaCounter(int hourlyQuota, TimeProvider timeProvider)
{
    private readonly Queue<DateTimeOffset> _requests = new();
    private readonly Lock _lock = new();

    public bool TryConsume()
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            while (_requests.Count > 0 && now - _requests.Peek() >= TimeSpan.FromHours(1))
            {
                _requests.Dequeue();
            }

            if (_requests.Count >= hourlyQuota)
            {
                return false;
            }

            _requests.Enqueue(now);
            return true;
        }
    }
}
