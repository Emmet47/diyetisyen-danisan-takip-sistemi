using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// Yalnızca "dotnet ef" komutları (migrations add / remove / database update) kullanır; API bunu kullanmaz.
/// Migration üretmek için veritabanına bağlanılmaz, bu yüzden user-secrets ayarlı olmasa da çalışır.
/// "dotnet ef database update" için ConnectionStrings__Default ortam değişkeni verilebilir;
/// normalde buna gerek yok, Development'ta API açılırken migration'ları kendisi uygular.
/// </summary>
public sealed class DdtsDbContextFactory : IDesignTimeDbContextFactory<DdtsDbContext>
{
    public DdtsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=ddts;Username=ddts";

        var options = new DbContextOptionsBuilder<DdtsDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DdtsDbContext(options);
    }
}
