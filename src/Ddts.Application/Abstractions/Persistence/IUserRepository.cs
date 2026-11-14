using Ddts.Domain.Users;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Giriş hesapları (users). Repository deseni, Şekil 4.10.</summary>
public interface IUserRepository
{
    Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>E-posta büyük/küçük harf farkı gözetmeden aranır (küçük harfe çevrilir).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Parola sıfırlama / davet bağlantısındaki token'ın hash'iyle kullanıcıyı bulur.</summary>
    Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    void Add(User user);
}
