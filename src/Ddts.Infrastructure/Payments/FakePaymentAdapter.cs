using Ddts.Application.Abstractions.Payments;

namespace Ddts.Infrastructure.Payments;

/// <summary>
/// IPaymentGateway'in simülasyon adapter'ı (Şekil 4.4, FR-L4): kart bilgisi almaz, gerçek ödeme yapmaz,
/// her zaman başarılı döner. Gerçek bir ödeme sağlayıcısı gelirse yalnızca yeni bir adapter yazılır.
/// </summary>
public sealed class FakePaymentAdapter : IPaymentGateway
{
    public Task<PaymentResult> ChargeAsync(Guid dietitianId, decimal amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(PaymentResult.Success($"SIM-{Guid.CreateVersion7():N}"));
}
