using Ddts.Application.Abstractions.Security;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ddts.Infrastructure.Persistence;

public static class AdminSeeder
{
    /// <summary>
    /// İlk platform yöneticisini oluşturur (rol-yetki matrisi: "ilk yönetici seed ile gelir").
    /// "Seed:AdminEmail" ve "Seed:AdminPassword" ayarlıysa ve henüz hiç yönetici yoksa çalışır.
    /// Parola koda yazılmaz; yerelde user-secrets, Docker'da .env (ADMIN_EMAIL, ADMIN_PASSWORD).
    /// </summary>
    public static async Task SeedAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = new User(email, UserRole.Admin);
        admin.SetPassword(passwordHasher.Hash(password));

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
    }
}
