using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Controllers;

//Idor Koruması: Aşı kaydının kendisinde SahipId yok, sahiplik hayvan üzerinden kontrol edilir:
// a.EvcilHayvan.SahipId == KullaniciId.EF bunu SQL'de Join'e çevirir
[Authorize(Roles = Roller.Uye)]
public class AsiKaydiController : Controller
{
    private readonly UygulamaDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public AsiKaydiController(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    private string KullaniciId => _kullaniciYoneticisi.GetUserId(User)!;

    // Sadece giriş yapan kullanıcının hayvanlarına ait aşı kayıtları
    private IQueryable<AsiKaydi> KullanicininAsiKayitlari() =>
        _db.AsiKayitlari.Where(a => a.EvcilHayvan!.SahipId == KullaniciId);

    // Hayvan bu kullanıcının mı? Değilse null döner.
    private Task<EvcilHayvan?> KullanicininHayvaniAsync(int evcilHayvanId) =>
        _db.EvcilHayvanlar
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == evcilHayvanId && h.SahipId == KullaniciId);

    public async Task<IActionResult> Index()
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        var sinir = bugun.AddDays(60);

        var takvim = await KullanicininAsiKayitlari()
            .AsNoTracking()
            .Where(a => a.SonrakiDozTarihi != null && a.SonrakiDozTarihi <= sinir)
            // Aynı aşı sonradan tekrar yapıldıysa eski kaydın "sonraki doz" tarihi artık geçersizdir.
            // Sadece her aşının en son kaydını al: "bundan daha yeni aynı aşı kaydı yoksa"
            .Where(a => !_db.AsiKayitlari.Any(b =>
                b.EvcilHayvanId == a.EvcilHayvanId &&
                b.AsiAdi == a.AsiAdi &&
                b.YapilmaTarihi > a.YapilmaTarihi))
            .OrderBy(a => a.SonrakiDozTarihi)
            .Select(a => new AsiTakvimiOgesi
            {
                EvcilHayvanId = a.EvcilHayvanId,
                HayvanAdi = a.EvcilHayvan!.Ad,
                AsiAdi = a.AsiAdi,
                SonYapilma = a.YapilmaTarihi,
                SonrakiDoz = a.SonrakiDozTarihi!.Value
            })
            .ToListAsync();

        return View(takvim);
    }

    public async Task<IActionResult> Ekle(int evcilHayvanId)
    {
        var hayvan = await KullanicininHayvaniAsync(evcilHayvanId);
        if (hayvan is null)
        {
            return NotFound();
        }

        return View(new AsiKaydiFormViewModel { EvcilHayvanId = hayvan.Id, HayvanAdi = hayvan.Ad });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(AsiKaydiFormViewModel model)
    {
        // EvcilHayvanId gizli alandan geliyor: değiştirilmiş olabilir, tekrar kontrol et!
        var hayvan = await KullanicininHayvaniAsync(model.EvcilHayvanId);
        if (hayvan is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.HayvanAdi = hayvan.Ad;
            return View(model);
        }

        _db.AsiKayitlari.Add(new AsiKaydi
        {
            EvcilHayvanId = hayvan.Id,
            AsiAdi = model.AsiAdi.Trim(),
            YapilmaTarihi = model.YapilmaTarihi,
            SonrakiDozTarihi = model.SonrakiDozTarihi,
            Notlar = model.Notlar?.Trim()
        });
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{hayvan.Ad} için \"{model.AsiAdi}\" kaydı eklendi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = hayvan.Id });
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var kayit = await KullanicininAsiKayitlari()
            .AsNoTracking()
            .Include(a => a.EvcilHayvan)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        return View(new AsiKaydiFormViewModel
        {
            Id = kayit.Id,
            EvcilHayvanId = kayit.EvcilHayvanId,
            HayvanAdi = kayit.EvcilHayvan!.Ad,
            AsiAdi = kayit.AsiAdi,
            YapilmaTarihi = kayit.YapilmaTarihi,
            SonrakiDozTarihi = kayit.SonrakiDozTarihi,
            Notlar = kayit.Notlar
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, AsiKaydiFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var kayit = await KullanicininAsiKayitlari()
            .Include(a => a.EvcilHayvan)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (kayit is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.HayvanAdi = kayit.EvcilHayvan!.Ad;
            return View(model);
        }

        // EvcilHayvanId değiştirilmez: kayıt başka bir hayvana taşınamaz
        kayit.AsiAdi = model.AsiAdi.Trim();
        kayit.YapilmaTarihi = model.YapilmaTarihi;
        kayit.SonrakiDozTarihi = model.SonrakiDozTarihi;
        kayit.Notlar = model.Notlar?.Trim();
        await _db.SaveChangesAsync();

        TempData["Basari"] = "Aşı kaydı güncellendi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = kayit.EvcilHayvanId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var kayit = await KullanicininAsiKayitlari().FirstOrDefaultAsync(a => a.Id == id);
        if (kayit is null)
        {
            return NotFound();
        }

        _db.AsiKayitlari.Remove(kayit);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{kayit.AsiAdi}\" kaydı silindi.";
        return RedirectToAction("Detay", "EvcilHayvan", new { id = kayit.EvcilHayvanId });
    }
}