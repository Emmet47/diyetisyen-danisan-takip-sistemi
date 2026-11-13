namespace Ddts.Domain.Common;

/// <summary>
/// İş kuralı ihlali (ör. "yayımlanmış plan değiştirilemez", "porsiyon 0'dan büyük olmalı").
/// API katmanında 400 Bad Request (ProblemDetails) olarak döner (docs/api.md 8.1).
/// Builder gibi birden çok hatayı birlikte bildiren yerler Errors listesini doldurur (Şekil 4.6).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
        Errors = [message];
    }

    public DomainException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Kullanıcıya gösterilecek hata mesajları.</summary>
    public IReadOnlyList<string> Errors { get; }
}
