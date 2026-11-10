using Ddts.Domain.Common;

namespace Ddts.UnitTests;

/// <summary>
/// AggregateRoot'un bekleyen olay listesini doğru tuttuğunu gösterir (Observer / Domain Events, Şekil 4.11).
/// Gerçek iş kuralı testleri (ör. "alerjisi olan besin plana eklenemez") bu klasöre eklenecek.
/// </summary>
public sealed class AggregateRootTests
{
    private sealed record OrnekOlay(DateTime OccurredAt) : IDomainEvent;

    private sealed class OrnekKok : AggregateRoot
    {
        public void BirSeyYap() => Raise(new OrnekOlay(DateTime.UtcNow));
    }

    [Fact]
    public void Raise_edilen_olay_bekleyen_olaylara_eklenir()
    {
        var kok = new OrnekKok();

        kok.BirSeyYap();

        Assert.Single(kok.DomainEvents);
    }

    [Fact]
    public void ClearEvents_bekleyen_olaylari_bosaltir()
    {
        var kok = new OrnekKok();
        kok.BirSeyYap();

        kok.ClearEvents();

        Assert.Empty(kok.DomainEvents);
    }
}
