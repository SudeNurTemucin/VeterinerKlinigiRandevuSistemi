using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class HizmetController : Controller
{
    private readonly UygulamaDbContext _db;

    public HizmetController(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? klinikId)
    {
        //ToListAsync() çağrılana kadar veritabanına gidilmez.
        IQueryable<Hizmet> sorgu = _db.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik);

        if (klinikId.HasValue)
        {
            sorgu = sorgu.Where(h => h.KlinikId == klinikId.Value);
        }

        var hizmetler = await sorgu
            .OrderBy(h => h.Klinik!.Ad)
            .ThenBy(h => h.Ad)
            .ToListAsync();

        await KlinikListesiniHazirlaAsync(klinikId);
        return View(hizmetler);
    }

    public async Task<IActionResult> Detay(int id)
    {
        var hizmet = await _db.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .Include(h => h.Veterinerler)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hizmet is null)
        {
            return NotFound();
        }
        return View(hizmet);
    }


    public async Task<IActionResult> Ekle(int? klinikId)
    {
        if (!await _db.Klinikler.AnyAsync())
        {
            TempData["Hata"] = "Hizmet eklemeden önce en az bir klinik eklemelisiniz.";
            return RedirectToAction("Ekle", "Klinik");
        }

        await KlinikListesiniHazirlaAsync(klinikId);
        return View(new Hizmet { SureDakika = 30, KlinikId = klinikId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(
        [Bind("Ad,Aciklama,SureDakika,Ucret,KlinikId")] Hizmet hizmet)
    {
        await KlinigiDogrulaAsync(hizmet.KlinikId);
        if (!ModelState.IsValid)
        {
            await KlinikListesiniHazirlaAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        _db.Hizmetler.Add(hizmet);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{hizmet.Ad}\" hizmeti eklendi.";
        return RedirectToAction(nameof(Index));
    }
 
    public async Task<IActionResult> Duzenle(int id)
    {
        var hizmet = await _db.Hizmetler.FindAsync(id);
        if (hizmet is null)
        {
            return NotFound();
        }

        await KlinikListesiniHazirlaAsync(hizmet.KlinikId);
        return View(hizmet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id,
        [Bind("Id,Ad,Aciklama,SureDakika,Ucret,KlinikId")] Hizmet hizmet)
    {
        if (id != hizmet.Id)
        {
            return BadRequest();
        }
        if (!await _db.Hizmetler.AnyAsync(h => h.Id == id))
        {
            return NotFound();
        }

        await KlinigiDogrulaAsync(hizmet.KlinikId);
        if (!ModelState.IsValid)
        {
            await KlinikListesiniHazirlaAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        _db.Hizmetler.Update(hizmet);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{hizmet.Ad}\" hizmeti güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    
    public async Task<IActionResult> Sil(int id)
    {
        var hizmet = await _db.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hizmet is null)
        {
            return NotFound();
        }
        return View(hizmet);
    }


    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnaylandi(int id)
    {
        var hizmet = await _db.Hizmetler.FindAsync(id);
        if (hizmet is null)
        {
            return NotFound();
        }

        if (await _db.Randevular.AnyAsync(r => r.HizmetId == id))
        {
            TempData["Hata"] = $"\"{hizmet.Ad}\" hizmetine ait randevular olduğu için silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _db.Hizmetler.Remove(hizmet);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{hizmet.Ad}\" hizmeti silindi.";
        return RedirectToAction(nameof(Index));
    }


    // Açılır liste için klinikleri hazırlar.
    private async Task KlinikListesiniHazirlaAsync(int? seciliKlinikId)
    {
        var klinikler = await _db.Klinikler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => new { k.Id, k.Ad })   // sadece Id ve Ad kolonları çekilir
            .ToListAsync();

        ViewBag.Klinikler = new SelectList(klinikler, "Id", "Ad", seciliKlinikId);
    }

    // Formdan gelen KlinikId gerçekten var olup olmadığını kontrol eder. Yoksa ModelState'e hata ekler.
    private async Task KlinigiDogrulaAsync(int klinikId)
    {
        if (!await _db.Klinikler.AnyAsync(k => k.Id == klinikId))
        {
            ModelState.AddModelError(nameof(Hizmet.KlinikId), "Lütfen geçerli bir klinik seçiniz.");
        }
    }
}