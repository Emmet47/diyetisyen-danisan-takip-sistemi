namespace Ddts.Application.Abstractions.Payments;

/// <summary>
/// Giden port (Şekil 4.4): abonelik ücretini tahsil eder. İlk sürümde FakePaymentAdapter
/// (ödeme simülasyonu, kart bilgisi alınmaz, FR-L4).
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(Guid dietitianId, decimal amount, CancellationToken cancellationToken = default);
}

/// <summary>Ödemenin sonucu.</summary>
/// <param name="Succeeded">Ödeme alındı mı?</param>
/// <param name="Reference">Ödeme işleminin numarası (simülasyonda "SIM-...").</param>
/// <param name="Error">Başarısızsa kullanıcıya gösterilecek mesaj.</param>
public sealed record PaymentResult(bool Succeeded, string? Reference, string? Error)
{
    public static PaymentResult Success(string reference) => new(true, reference, null);

    public static PaymentResult Failure(string error) => new(false, null, error);
}
