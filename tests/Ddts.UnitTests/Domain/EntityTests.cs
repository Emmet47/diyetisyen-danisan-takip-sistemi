using Ddts.Domain.Common;

namespace Ddts.UnitTests;

/// <summary>
/// Örnek birim testi: test altyapısının çalıştığını gösterir.
/// Gerçek iş kuralı testleri (ör. "alerjisi olan besin plana eklenemez") bu klasöre eklenecek.
/// </summary>
public sealed class EntityTests
{
    private sealed record OrnekEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class OrnekVarlik : Entity
    {
        public void BirSeyYap() => Raise(new OrnekEvent(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Yeni_varliklarin_kimlikleri_birbirinden_farkli()
    {
        Assert.NotEqual(new OrnekVarlik().Id, new OrnekVarlik().Id);
    }

    [Fact]
    public void PullDomainEvents_eventleri_dondurur_ve_listeyi_bosaltir()
    {
        var varlik = new OrnekVarlik();
        varlik.BirSeyYap();

        Assert.Single(varlik.PullDomainEvents());
        Assert.Empty(varlik.PullDomainEvents());
    }
}
