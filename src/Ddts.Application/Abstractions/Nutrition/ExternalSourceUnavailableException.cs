namespace Ddts.Application.Abstractions.Nutrition;

/// <summary>
/// Dış besin kaynağı kullanılamıyor: zaman aşımı (3 sn), ağ/sunucu hatası, 429 ya da saatlik kota dolu (FR-D4).
/// Ayrıntı adapter'da loglanır; kullanıcıya yalnızca genel mesaj gösterilir.
/// </summary>
public sealed class ExternalSourceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
