using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Data
{
    public class BaslangicVerisi
    {
        private const string AdminEposta = "b241210108@sakarya.edu.tr";
        private const string AdminSifre = "sau";

        public static async Task YukleAsync(IServiceProvider servisler)
        {
            var db = servisler.GetRequiredService<UygulamaDbContext>();
            var rolYoneticisi = servisler.GetRequiredService<RoleManager<IdentityRole>>();
            var kullaniciYoneticisi = servisler.GetRequiredService<UserManager<Kullanici>>();

            //Bekleyen migration'ları uygula.
            await db.Database.MigrateAsync();

            //Rolleri oluştur, roller zaten varsa atla.
            foreach (var rol in Roller.Tumu)
            {
                if (!await rolYoneticisi.RoleExistsAsync(rol))
                {
                    await rolYoneticisi.CreateAsync(new IdentityRole(rol));
                }
            }

            //Admin hesabını oluştur, admin zaten varsa atla.
            if (await kullaniciYoneticisi.FindByEmailAsync(AdminEposta) is null)
            {
                var admin = new Kullanici
                {
                    UserName = AdminEposta, //Kullanıcı adını e-posta ile aynı tutuyoruz. Böylece kullanıcılar e-postalarıyla giriş yapabiliyor.
                    Email = AdminEposta,
                    EmailConfirmed = true,
                    Ad = "Sistem",
                    Soyad = "Yöneticisi"
                };

                var sonuc = await kullaniciYoneticisi.CreateAsync(admin, AdminSifre); //Şifreyi parametre olarak veriyoruz, çünkü Identity şifreyi hash'leyip öyle saklıyor.
                if (!sonuc.Succeeded)
                {
                    var hatalar = string.Join(", ", sonuc.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Admin oluşturulamadı: {hatalar}");
                }

                await kullaniciYoneticisi.AddToRoleAsync(admin, Roller.Admin);
            }
            //Tablo boşsa varsayılan uzmanlık alanları eklenir.
            if (!await db.UzmanlikAlanlari.AnyAsync())
            {
                string[] alanlar =
                {
                "Dahiliye", "Cerrahi", "Dermatoloji", "Ortopedi",
                "Diş Hekimliği", "Göz Hastalıkları", "Kardiyoloji", "Egzotik Hayvanlar"
            };

                db.UzmanlikAlanlari.AddRange(alanlar.Select(ad => new UzmanlikAlani { Ad = ad }));
                await db.SaveChangesAsync();

            }
        }
    }
}