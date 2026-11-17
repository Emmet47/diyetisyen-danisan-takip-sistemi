namespace Ddts.Application.Common;

// Servislerin fırlattığı hatalar. API katmanı bunları HTTP durum kodlarına çevirir (docs/api.md 8.1):
//   DomainException ve InvalidPhotoException → 400, AuthenticationFailedException → 401,
//   ForbiddenException → 403, NotFoundException → 404,
//   ConflictException, SlotTakenException ve SubscriptionLimitException → 409,
//   ExternalSourceUnavailableException (yalnız içe aktarmada dışarı çıkar) → 503.

/// <summary>Kayıt yok ya da isteği yapana ait değil (404). İki durum ayırt edilmez: kaydın varlığı belli olmaz.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Mevcut durumla çakışma (409), ör. e-posta zaten kayıtlı.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Giriş başarısız (401): e-posta ya da parola hatalı, hesap pasif.</summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);

/// <summary>Kayıt görünüyor ama bu işlem yapılamıyor (ör. ortak besini düzenleme, FR-D2) → 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);

/// <summary>Seçilen randevu saati az önce başka bir randevuyla doldu (FR-I4, Şekil 5.7) → 409.</summary>
public sealed class SlotTakenException(string message) : Exception(message);

/// <summary>Yüklenen dosya JPEG/PNG değil ya da 5 MB'tan büyük (BR-10) → 400 "Geçersiz dosya".</summary>
public sealed class InvalidPhotoException(string message) : Exception(message);
