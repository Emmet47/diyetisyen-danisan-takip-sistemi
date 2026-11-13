using Ddts.Domain.Common;

namespace Ddts.Domain.Plans;

/// <summary>
/// Plan yayımlandı olayı (Şekil 4.11). Kayıttan sonra PlanPublishedHandler danışana
/// "Yeni planın hazır" bildirimi üretir (FR-E4, FR-J1).
/// </summary>
public sealed record PlanPublished(Guid PlanId, Guid DietitianId, Guid ClientId, DateTime OccurredAt) : IDomainEvent;
