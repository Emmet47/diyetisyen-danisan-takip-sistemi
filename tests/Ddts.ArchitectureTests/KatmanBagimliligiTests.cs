using System.Reflection;
using NetArchTest.Rules;

namespace Ddts.ArchitectureTests;

/// <summary>
/// AT-01 — Bağımlılık kuralı (Dependency Rule): kaynak kod bağımlılıkları hep içe doğru.
/// Domain hiçbir projeye bağlı değil; Application yalnızca Domain'e bağlı.
/// Ddts.Domain ve Ddts.Application; EF Core, ASP.NET Core, AWS SDK ve Firebase paketlerine bağlanamaz.
/// Biri yanlışlıkla Domain'e EF Core eklerse bu testler CI'da kırılır.
/// </summary>
public sealed class KatmanBagimliligiTests
{
    private static readonly Assembly DomainAssembly = typeof(Ddts.Domain.Common.Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Ddts.Application.Abstractions.Persistence.IUnitOfWork).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Ddts.Infrastructure.Persistence.AppDbContext).Assembly;

    // Çekirdeğin (Domain + Application) bilmemesi gereken dış dünya paketleri
    private static readonly string[] DisPaketler =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
        "Amazon",        // AWS SDK (S3 / SeaweedFS)
        "FirebaseAdmin", // FCM
        "QuestPDF",
    ];

    [Fact]
    public void Domain_diger_katmanlara_ve_dis_paketlere_bagli_degil()
    {
        var sonuc = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Ddts.Application", "Ddts.Infrastructure", "Ddts.Api", .. DisPaketler])
            .GetResult();

        Assert.True(sonuc.IsSuccessful, KuraliBozanlar(sonuc));
    }

    [Fact]
    public void Application_altyapiya_ve_dis_paketlere_bagli_degil()
    {
        var sonuc = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Ddts.Infrastructure", "Ddts.Api", .. DisPaketler])
            .GetResult();

        Assert.True(sonuc.IsSuccessful, KuraliBozanlar(sonuc));
    }

    [Fact]
    public void Infrastructure_api_katmanina_bagli_degil()
    {
        var sonuc = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Ddts.Api")
            .GetResult();

        Assert.True(sonuc.IsSuccessful, KuraliBozanlar(sonuc));
    }

    // Tam ad: xUnit'te de "TestResult" adlı bir tip olabileceği için karışmasın.
    private static string KuraliBozanlar(NetArchTest.Rules.TestResult sonuc) =>
        "Kuralı bozan tipler: " + string.Join(", ", sonuc.FailingTypeNames ?? Enumerable.Empty<string>());
}
