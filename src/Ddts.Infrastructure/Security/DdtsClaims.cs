namespace Ddts.Infrastructure.Security;

/// <summary>JWT içindeki claim (bilgi alanı) adları. Token üreten ve okuyan taraf aynı adları kullanır.</summary>
public static class DdtsClaims
{
    /// <summary>Kullanıcı kimliği (users.id).</summary>
    public const string Subject = "sub";

    /// <summary>Admin, Dietitian ya da Client.</summary>
    public const string Role = "role";

    /// <summary>Diyetisyende kendi kimliği, danışanda bağlı olduğu diyetisyen.</summary>
    public const string DietitianId = "dietitian_id";

    /// <summary>Yalnızca danışanda.</summary>
    public const string ClientId = "client_id";
}
