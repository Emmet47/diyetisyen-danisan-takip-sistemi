using Ddts.Domain.Common;

namespace Ddts.Domain.Subscriptions;

/// <summary>
/// Paket limiti dolu ya da abonelik aktif değil (BR-01, Şekil 5.1).
/// Diğer iş kuralı hatalarından farklı olarak API'de 409 Conflict'e çevrilir.
/// </summary>
public sealed class SubscriptionLimitException(string message) : DomainException(message);
