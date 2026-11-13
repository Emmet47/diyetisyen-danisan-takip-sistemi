using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.MealLogs;
using Ddts.Domain.Measurements;
using Ddts.Domain.Notifications;
using Ddts.Domain.Users;

namespace Ddts.UnitTests.Domain;

/// <summary>Kullanıcı hesabı, danışan profili, bildirim izinleri (UT-11), ölçüm ve öğün fotoğrafı (UT-10).</summary>
public sealed class KullaniciVeDanisanTests
{
    [Fact]
    public void E_posta_kucuk_harfle_ve_bosluksuz_saklanir()
    {
        var kullanici = new User("  Ayse.Dyt@Ornek.Test ", UserRole.Dietitian);

        Assert.Equal("ayse.dyt@ornek.test", kullanici.Email);
        Assert.True(kullanici.IsActive);
        Assert.Null(kullanici.PasswordHash);
        Assert.Throws<DomainException>(() => new User("gecersiz", UserRole.Client));
    }

    [Fact]
    public void Sifirlama_baglantisi_suresi_icinde_gecerli_parola_degisince_gecersiz()
    {
        var kullanici = new User("danisan.a@ornek.test", UserRole.Client);
        kullanici.StartPasswordReset("token-hash", TestVerisi.Simdi.AddHours(1));

        Assert.True(kullanici.IsResetTokenValid("token-hash", TestVerisi.Simdi));
        Assert.False(kullanici.IsResetTokenValid("baska-hash", TestVerisi.Simdi));
        Assert.False(kullanici.IsResetTokenValid("token-hash", TestVerisi.Simdi.AddHours(2)));

        kullanici.SetPassword("parola-hash");

        Assert.Equal("parola-hash", kullanici.PasswordHash);
        Assert.False(kullanici.IsResetTokenValid("token-hash", TestVerisi.Simdi));
    }

    [Fact]
    public void Yeni_danisanda_butun_bildirim_izinleri_acik()
    {
        var danisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());

        Assert.True(danisan.IsActive);
        Assert.Equal(NotificationSettings.AllEnabled, danisan.Notifications);
        Assert.All(Enum.GetValues<NotificationType>(), tur => Assert.True(danisan.AllowsNotification(tur)));
    }

    [Fact]
    public void Yas_dogum_gunu_gelmediyse_bir_eksik_hesaplanir()
    {
        var danisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());   // doğum: 1 Ocak 1995

        Assert.Equal(31, danisan.AgeOn(new DateOnly(2026, 12, 1)));
        Assert.Equal(30, danisan.AgeOn(new DateOnly(2025, 12, 31)));
        Assert.Equal(31, danisan.AgeOn(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Profil_guncellenir_hedef_kilo_sinir_disinda_olamaz()
    {
        var danisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());

        danisan.UpdateProfile(new ClientProfile("Yeni Ad", "05550000000", GoalType.Maintain, 60m, "Vejetaryen", null));

        Assert.Equal("Yeni Ad", danisan.FullName);
        Assert.Equal(GoalType.Maintain, danisan.GoalType);
        Assert.Null(danisan.GetRestriction(Guid.NewGuid()));
        Assert.Throws<DomainException>(() =>
            danisan.UpdateProfile(new ClientProfile("Yeni Ad", null, GoalType.Maintain, 10m, null, null)));
    }

    [Fact]
    public void Olcum_sinirlarin_disinda_girilemez()
    {
        var gun = new DateOnly(2026, 12, 1);

        var olcum = new Measurement(Guid.NewGuid(), gun, 70.4m, 168m, 82m, null, null, null);

        Assert.Equal(70.4m, olcum.WeightKg);
        Assert.Throws<DomainException>(() => new Measurement(Guid.NewGuid(), gun, 400m, 168m, 82m, null, null, null));
        Assert.Throws<DomainException>(() => new Measurement(Guid.NewGuid(), gun, 70m, 168m, 82m, null, 90m, null));
    }

    [Fact]
    public void Fotograf_yalnizca_JPEG_PNG_ve_en_fazla_5_MB()
    {
        Assert.Equal(PhotoInfo.MaxSizeBytes, PhotoInfo.Create("a.jpg", "image/jpeg", 5 * 1024 * 1024).SizeBytes);
        Assert.Throws<DomainException>(() => PhotoInfo.Create("a.gif", "image/gif", 1000));
        Assert.Throws<DomainException>(() => PhotoInfo.Create("b.png", "image/png", 5 * 1024 * 1024 + 1));
        Assert.Throws<DomainException>(() => PhotoInfo.Create("c.png", "image/png", 0));
    }

    [Fact]
    public void Ogun_kaydina_tek_fotograf_eklenir()
    {
        var kayit = new MealLog(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 12, 1), MealLogStatus.Partial, "Yarısını yedim");

        kayit.AttachPhoto(PhotoInfo.Create("meal-photos/1.jpg", "image/jpeg", 1000));

        Assert.NotNull(kayit.Photo);
        Assert.Throws<DomainException>(() => kayit.AttachPhoto(PhotoInfo.Create("meal-photos/2.jpg", "image/jpeg", 1000)));
    }
}
