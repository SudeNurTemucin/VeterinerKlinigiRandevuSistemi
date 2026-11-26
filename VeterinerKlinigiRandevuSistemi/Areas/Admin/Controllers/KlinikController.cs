using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class KlinikController : Controller
{
    private readonly UygulamaDbContext _db;

    public KlinikController(UygulamaDbContext db)
    {
        _db = db;
    }

    //Get:/Admin/Klinik
    public async Task<IActionResult> Index()
    {
        var klinikler = await _db.Klinikler
            .AsNoTracking()
            .Include(k => k.Hizmetler)
            .Include(k => k.Veterinerler)
            .OrderBy(k => k.Ad)
            .ToListAsync();

        return View(klinikler);
    }

    //Get:/Admin/Klinik/Detay/id
    public async Task<IActionResult> Detay(int id)
    {
        var klinik = await _db.Klinikler
            .AsNoTracking()
            .Include(k => k.Hizmetler)
            .Include(k => k.Veterinerler)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (klinik is null)
        {
            return NotFound();
        }
        return View(klinik);
    }

    //Get:/Admin/Klinik/Ekle
    public IActionResult Ekle() => View(new Klinik());

    //Post:/Admin/Klinik/Ekle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(
        [Bind("Ad,Adres,Telefon,AcilisSaati,KapanisSaati")] Klinik klinik)
    {
        if (!ModelState.IsValid)
        {
            return View(klinik);
        }

        _db.Klinikler.Add(klinik);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{klinik.Ad}\" kliniği eklendi.";
        return RedirectToAction(nameof(Index));
    }

    //Get:/Admin/Klinik/Duzenle/id
    public async Task<IActionResult> Duzenle(int id)
    {
        var klinik = await _db.Klinikler.FindAsync(id);
        if (klinik is null)
        {
            return NotFound();
        }
        return View(klinik);
    }

    //Post:/Admin/Klinik/Duzenle/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id,
        [Bind("Id,Ad,Adres,Telefon,AcilisSaati,KapanisSaati")] Klinik klinik)
    {
        if (id != klinik.Id)
        {
            return BadRequest();
        }
        if (!await _db.Klinikler.AnyAsync(k => k.Id == id))
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(klinik);
        }

        _db.Klinikler.Update(klinik);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{klinik.Ad}\" kliniği güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    //Get:/Admin/Klinik/Sil/id  (onay sayfası)
    public async Task<IActionResult> Sil(int id)
    {
        var klinik = await _db.Klinikler.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
        if (klinik is null)
        {
            return NotFound();
        }
        return View(klinik);
    }

    //Post:/Admin/Klinik/Sil/id
    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnaylandi(int id)
    {
        var klinik = await _db.Klinikler.FindAsync(id);
        if (klinik is null)
        {
            return NotFound();
        }

        bool bagliKayitVar = await _db.Hizmetler.AnyAsync(h => h.KlinikId == id)
                          || await _db.Veterinerler.AnyAsync(v => v.KlinikId == id);
        if (bagliKayitVar)
        {
            TempData["Hata"] = $"\"{klinik.Ad}\" kliniğine bağlı hizmet veya veteriner olduğu için silinemez. Önce onları kaldırın.";
            return RedirectToAction(nameof(Index));
        }

        _db.Klinikler.Remove(klinik);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{klinik.Ad}\" kliniği silindi.";
        return RedirectToAction(nameof(Index));
    }
}
