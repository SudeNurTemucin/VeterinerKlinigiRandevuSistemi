using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.Services;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Controllers;

[Authorize(Roles = Roller.Uye)]
public class RandevuController : Controller
{
    private readonly UygulamaDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;
    private readonly IRandevuServisi _randevuServisi;

    public RandevuController(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi,
        IRandevuServisi randevuServisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
        _randevuServisi = randevuServisi;
    }

    private string KullaniciId => _kullaniciYoneticisi.GetUserId(User)!;

    //Randevu listesi
    public async Task<IActionResult> Index()
    {
        var randevular = await _randevuServisi.KullanicininRandevulariAsync(KullaniciId);
        return View(randevular);
    }

    //Randevu alma sayfası
    public async Task<IActionResult> Al(int? evcilHayvanId)
    {
        var model = new RandevuAlViewModel
        {
            EvcilHayvanId = evcilHayvanId ?? 0,
            Tarih = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };

        await SecenekleriDoldurAsync(model);
        if (model.Hayvanlar.Count == 0)
        {
            TempData["Hata"] = "Randevu alabilmek için önce bir evcil hayvan eklemelisiniz.";
            return RedirectToAction("Ekle", "EvcilHayvan");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Al(RandevuAlViewModel model)
    {
        if (ModelState.IsValid)
        {
            var talep = new RandevuTalebi(
                KullaniciId,
                model.EvcilHayvanId,
                model.VeterinerId,
                model.HizmetId,
                model.Tarih.ToDateTime(model.Saat!.Value),
                model.Aciklama);

            var sonuc = await _randevuServisi.RandevuOlusturAsync(talep);
            if (sonuc.Basarili)
            {
                TempData["Basari"] = sonuc.Mesaj;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
        }

        await SecenekleriDoldurAsync(model);
        return View(model);
    }

    //Randevu iptal etme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var sonuc = await _randevuServisi.RandevuIptalEtAsync(KullaniciId, id);
        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    //JQuery AJAX çağrıları için JSON döndüren metodlar

    //Sadece en az bir veterinerin verdiği hizmetler
    [HttpGet]
    public async Task<IActionResult> Hizmetler(int klinikId)
    {
        var hizmetler = await _db.Hizmetler
            .AsNoTracking()
            .Where(h => h.KlinikId == klinikId && h.Veterinerler.Any())
            .OrderBy(h => h.Ad)
            .Select(h => new { h.Id, h.Ad, h.SureDakika, h.Ucret })
            .ToListAsync();

        return Json(hizmetler.Select(h => new { h.Id, h.Ad, h.SureDakika, UcretMetni = h.Ucret.ToString("C") }));
    }

    //Randevu alırken seçilen hizmeti veren veterinerler
    [HttpGet]
    public async Task<IActionResult> Veterinerler(int hizmetId)
    {
        var veterinerler = await _db.Veterinerler
            .AsNoTracking()
            .Where(v => v.Hizmetler.Any(h => h.Id == hizmetId))
            .OrderBy(v => v.Ad)
            .Select(v => new { v.Id, v.Ad, v.Soyad })
            .ToListAsync();

        return Json(veterinerler.Select(v => new { v.Id, Ad = $"Dr. {v.Ad} {v.Soyad}" }));
    }

    //Randevu alırken seçilen veterinerin, seçilen hizmet için, seçilen gündeki boş saatleri
    [HttpGet]
    public async Task<IActionResult> MusaitSaatler(int veterinerId, int hizmetId, DateOnly tarih)
    {
        var saatler = await _randevuServisi.MusaitSaatleriGetirAsync(veterinerId, hizmetId, tarih);
        return Json(saatler.Select(s => s.ToString("HH:mm")));
    }

    private async Task SecenekleriDoldurAsync(RandevuAlViewModel model)
    {
        var hayvanlar = await _db.EvcilHayvanlar
            .AsNoTracking()
            .Where(h => h.SahipId == KullaniciId)
            .OrderBy(h => h.Ad)
            .Select(h => new { h.Id, h.Ad, h.Tur })
            .ToListAsync();
        model.Hayvanlar = hayvanlar
            .Select(h => new SelectListItem($"{h.Ad} ({h.Tur})", h.Id.ToString()))
            .ToList();

        var klinikler = await _db.Klinikler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => new { k.Id, k.Ad })
            .ToListAsync();
        model.Klinikler = klinikler
            .Select(k => new SelectListItem(k.Ad, k.Id.ToString()))
            .ToList();
    }
}