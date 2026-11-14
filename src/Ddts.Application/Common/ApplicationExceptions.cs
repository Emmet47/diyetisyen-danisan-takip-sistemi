namespace Ddts.Application.Common;

// Servislerin fırlattığı hatalar. API katmanı bunları HTTP durum kodlarına çevirir (docs/api.md 8.1):
//   DomainException → 400, AuthenticationFailedException → 401, NotFoundException → 404,
//   ConflictException ve SubscriptionLimitException → 409.

/// <summary>Kayıt yok ya da isteği yapana ait değil (404). İki durum ayırt edilmez: kaydın varlığı belli olmaz.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Mevcut durumla çakışma (409), ör. e-posta zaten kayıtlı.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Giriş başarısız (401): e-posta ya da parola hatalı, hesap pasif.</summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);
