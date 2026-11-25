using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Controllers;

public class HesapController : Controller
{
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;
    private readonly SignInManager<Kullanici> _girisYoneticisi;

    public HesapController(UserManager<Kullanici> kullaniciYoneticisi,
                           SignInManager<Kullanici> girisYoneticisi)
    {
        _kullaniciYoneticisi = kullaniciYoneticisi;
        _girisYoneticisi = girisYoneticisi;
    }

    [HttpGet]
    public IActionResult Giris(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) //Sunucu tarafında model doğrulaması, client tarafında doğrulama yapılmasa bile çalışır.
        {
            return View(model);
        }

        var sonuc = await _girisYoneticisi.PasswordSignInAsync(
            model.Eposta, model.Sifre, model.BeniHatirla, lockoutOnFailure: true);

        if (sonuc.Succeeded)
        {
            // Sadece kendi sitemizdeki adreslere geri dönmek için Open Redirect koruması
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, sonuc.IsLockedOut
            ? "Çok fazla hatalı deneme. Lütfen birkaç dakika sonra tekrar deneyin."
            : "E-posta veya şifre hatalı.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Kayit() => View();

    [HttpPost]
    [ValidateAntiForgeryToken] //CSRF saldırısına karşı koruma.
    //Form tag helper her forma gizli bir token ekle; braşka bir siteden gönderilen sahte formda bu token olmadığı için istek reddediliyor.
    public async Task<IActionResult> Kayit(KayitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var kullanici = new Kullanici
        {
            UserName = model.Eposta,
            Email = model.Eposta,
            Ad = model.Ad,
            Soyad = model.Soyad
        };

        var sonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, model.Sifre);
        if (sonuc.Succeeded)
        {
            // Dışarıdan kayıt olan herkes sadec Uye rolünü alır
            await _kullaniciYoneticisi.AddToRoleAsync(kullanici, Roller.Uye);
            await _girisYoneticisi.SignInAsync(kullanici, isPersistent: false);

            TempData["Basari"] = $"Hoş geldin {kullanici.Ad}, kaydın tamamlandı!";
            return RedirectToAction("Index", "Home");
        }

        foreach (var hata in sonuc.Errors)
        {
            ModelState.AddModelError(string.Empty, hata.Description);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Cikis()
    {
        await _girisYoneticisi.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ErisimEngellendi() => View();
}