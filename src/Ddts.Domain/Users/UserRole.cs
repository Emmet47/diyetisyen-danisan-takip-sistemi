namespace Ddts.Domain.Users;

/// <summary>Kullanıcı rolü (Şekil 4.3). Veritabanında metin olarak saklanır: users.role.</summary>
public enum UserRole
{
    Admin,
    Dietitian,
    Client,
}
