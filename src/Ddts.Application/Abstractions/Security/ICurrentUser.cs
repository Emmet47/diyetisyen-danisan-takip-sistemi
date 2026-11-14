using Ddts.Domain.Users;

namespace Ddts.Application.Abstractions.Security;

/// <summary>
/// Giden port (Şekil 4.4): isteği yapan kullanıcının bilgileri. Uygulaması HttpCurrentUser (JWT claim'leri).
/// Servisler diyetisyen kimliğini (tenant) buradan alır; istemcinin gönderdiği bir değere güvenilmez (NFR-G1).
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    UserRole Role { get; }

    /// <summary>Diyetisyen için kendi kimliği, danışan için bağlı olduğu diyetisyen; yöneticide null.</summary>
    Guid? DietitianId { get; }

    /// <summary>Yalnızca danışanda dolu.</summary>
    Guid? ClientId { get; }
}
