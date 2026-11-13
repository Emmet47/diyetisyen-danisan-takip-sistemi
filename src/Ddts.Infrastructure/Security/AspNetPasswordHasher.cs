using Ddts.Application.Abstractions.Security;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Ddts.Infrastructure.Security;

/// <summary>
/// IPasswordHasher'ın adapter'ı (Şekil 4.4): ASP.NET Core'un PasswordHasher'ı, PBKDF2 (NFR-G2).
/// Hash'in içinde rastgele tuz (salt) ve tekrar sayısı saklanır; aynı parola her seferinde farklı hash verir.
/// (Bizim IPasswordHasher'ımız ile ASP.NET Core'un IPasswordHasher&lt;TUser&gt;'ı farklı arayüzler.)
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // ASP.NET Core'un hasher'ı kullanıcı nesnesini kullanmıyor; bu yüzden null veriyoruz.
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
