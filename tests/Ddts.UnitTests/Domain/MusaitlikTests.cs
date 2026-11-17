using Ddts.Domain.Appointments;
using Ddts.Domain.Dietitians;

namespace Ddts.UnitTests.Domain;

/// <summary>Boş slot hesabı (AvailabilityService, UT-09, BR-08).</summary>
public sealed class MusaitlikTests
{
    private static readonly DateOnly Sali = new(2026, 12, 1);   // 1 Aralık 2026 Salı → gün 2

    [Fact]
    public void Calisma_saati_30_dakikalik_slotlara_bolunur_saatler_UTC_doner()
    {
        var saatler = new[] { new WorkingHour(2, new TimeOnly(9, 0), new TimeOnly(11, 0)) };

        var slotlar = new AvailabilityService().GetFreeSlots(saatler, Sali, []);

        Assert.Equal(4, slotlar.Count);
        // 09:00 TSİ = 06:00 UTC
        Assert.Equal(new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc), slotlar[0].Start);
        Assert.Equal(new DateTime(2026, 12, 1, 6, 30, 0, DateTimeKind.Utc), slotlar[0].End);
        Assert.Equal(new DateTime(2026, 12, 1, 7, 30, 0, DateTimeKind.Utc), slotlar[^1].Start);
    }

    [Fact]
    public void Aktif_randevunun_slotu_dolu_iptal_edilenin_slotu_bos_sayilir()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var saatler = new[] { new WorkingHour(2, new TimeOnly(9, 0), new TimeOnly(11, 0)) };
        var onceki = new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);

        var talep = Appointment.Request(diyetisyen, danisan, new TimeSlot(onceki, onceki.AddMinutes(30)));
        var iptal = Appointment.Request(diyetisyen, danisan, new TimeSlot(onceki.AddHours(1), onceki.AddHours(1.5)));
        iptal.Cancel(Ddts.Domain.Users.UserRole.Client);

        var slotlar = new AvailabilityService().GetFreeSlots(saatler, Sali, [talep, iptal]);

        Assert.Equal(3, slotlar.Count);
        Assert.DoesNotContain(slotlar, s => s.Start == onceki);
        Assert.Contains(slotlar, s => s.Start == onceki.AddHours(1));
    }

    [Fact]
    public void Calisilmayan_gunde_slot_yoktur()
    {
        var saatler = new[] { new WorkingHour(1, new TimeOnly(9, 0), new TimeOnly(17, 0)) };   // yalnızca Pazartesi

        Assert.Empty(new AvailabilityService().GetFreeSlots(saatler, Sali, []));
    }
}
