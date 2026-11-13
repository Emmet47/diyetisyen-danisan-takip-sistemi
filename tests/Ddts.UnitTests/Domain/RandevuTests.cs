using Ddts.Domain.Appointments;
using Ddts.Domain.Common;
using Ddts.Domain.Users;

namespace Ddts.UnitTests.Domain;

/// <summary>Randevu durum geçişleri (UT-08, BR-09) ve zaman aralığı.</summary>
public sealed class RandevuTests
{
    private static readonly TimeSlot Slot = new(TestVerisi.Simdi, TestVerisi.Simdi.AddMinutes(30));

    [Fact]
    public void Danisan_talebi_Talep_Edildi_baslar_ve_olay_uretir()
    {
        var randevu = YeniTalep();

        Assert.Equal(AppointmentStatus.Requested, randevu.Status);
        Assert.IsType<AppointmentRequested>(Assert.Single(randevu.DomainEvents));
    }

    [Fact]
    public void Diyetisyen_onaylar_ikinci_kez_onaylanamaz()
    {
        var randevu = YeniTalep();
        randevu.ClearEvents();

        randevu.Confirm();

        Assert.Equal(AppointmentStatus.Confirmed, randevu.Status);
        Assert.IsType<AppointmentConfirmed>(Assert.Single(randevu.DomainEvents));
        Assert.Throws<DomainException>(randevu.Confirm);
    }

    [Fact]
    public void Iptal_olayi_iptal_eden_tarafi_tasir_ve_iptal_edilen_degismez()
    {
        var randevu = YeniTalep();
        randevu.ClearEvents();

        randevu.Cancel(UserRole.Client);

        Assert.Equal(AppointmentStatus.Cancelled, randevu.Status);
        var olay = Assert.IsType<AppointmentCancelled>(Assert.Single(randevu.DomainEvents));
        Assert.Equal(UserRole.Client, olay.By);
        Assert.Throws<DomainException>(() => randevu.Reschedule(Slot, UserRole.Dietitian));
    }

    [Fact]
    public void Danisan_ertelerse_yeniden_talep_diyetisyen_ertelerse_onayli()
    {
        var yeniSlot = new TimeSlot(TestVerisi.Simdi.AddDays(1), TestVerisi.Simdi.AddDays(1).AddMinutes(30));
        var randevu = YeniTalep();
        randevu.Confirm();

        randevu.Reschedule(yeniSlot, UserRole.Client);
        Assert.Equal(AppointmentStatus.Requested, randevu.Status);
        Assert.Equal(yeniSlot.Start, randevu.StartsAt);

        randevu.Reschedule(Slot, UserRole.Dietitian);
        Assert.Equal(AppointmentStatus.Confirmed, randevu.Status);
    }

    [Fact]
    public void Tamamlandi_yalnizca_onayli_ve_saati_gelmis_randevuda()
    {
        var randevu = YeniTalep();
        Assert.Throws<DomainException>(() => randevu.Complete(TestVerisi.Simdi.AddHours(1)));

        randevu.Confirm();
        Assert.Throws<DomainException>(() => randevu.Complete(TestVerisi.Simdi.AddMinutes(-5)));

        randevu.Complete(TestVerisi.Simdi.AddMinutes(30));
        Assert.Equal(AppointmentStatus.Completed, randevu.Status);
    }

    [Fact]
    public void Yonetici_randevu_degistiremez()
    {
        var randevu = YeniTalep();

        Assert.Throws<DomainException>(() => randevu.Cancel(UserRole.Admin));
    }

    [Fact]
    public void Baska_diyetisyenin_danisanina_randevu_olusturulamaz()
    {
        var danisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());

        Assert.Throws<DomainException>(() => Appointment.Request(TestVerisi.Diyetisyen(), danisan, Slot));
    }

    [Fact]
    public void Bitisik_slotlar_cakismaz_ic_ice_slotlar_cakisir()
    {
        var sonraki = new TimeSlot(Slot.End, Slot.End.AddMinutes(30));
        var icIce = new TimeSlot(Slot.Start.AddMinutes(15), Slot.End.AddMinutes(15));

        Assert.False(Slot.Overlaps(sonraki));
        Assert.True(Slot.Overlaps(icIce));
        Assert.Throws<DomainException>(() => new TimeSlot(Slot.End, Slot.Start));
        Assert.Throws<ArgumentException>(() => new TimeSlot(DateTime.Now, DateTime.Now.AddMinutes(30)));
    }

    private static Appointment YeniTalep()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        return Appointment.Request(diyetisyen, TestVerisi.Danisan(diyetisyen), Slot);
    }
}
