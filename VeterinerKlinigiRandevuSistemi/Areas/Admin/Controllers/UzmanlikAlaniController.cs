using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class UzmanlikAlaniController : Controller
{
    private readonly UygulamaDbContext _db;

    public UzmanlikAlaniController(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Projeksiyon + Count: her alandaki veteriner sayısını SQL'de hesaplar,
        var liste = await _db.UzmanlikAlanlari
            .AsNoTracking()
            .OrderBy(u => u.Ad)
            .Select(u => new UzmanlikAlaniListeOgesi
            {
                Id = u.Id,
                Ad = u.Ad,
                VeterinerSayisi = u.Veterinerler.Count
            })
            .ToListAsync();

        return View(liste);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle([Bind("Ad")] UzmanlikAlani alan)
    {
        alan.Ad = alan.Ad?.Trim() ?? string.Empty;

        if (!ModelState.IsValid || alan.Ad.Length == 0)
        {
            TempData["Hata"] = "Uzmanlık adı boş olamaz (en fazla 100 karakter).";
        }
        else if (await _db.UzmanlikAlanlari.AnyAsync(u => u.Ad == alan.Ad))
        {
            TempData["Hata"] = $"\"{alan.Ad}\" zaten listede var.";
        }
        else
        {
            _db.UzmanlikAlanlari.Add(alan);
            await _db.SaveChangesAsync();
            TempData["Basari"] = $"\"{alan.Ad}\" uzmanlık alanı eklendi.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var alan = await _db.UzmanlikAlanlari.FindAsync(id);
        if (alan is null)
        {
            return NotFound();
        }
        return View(alan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, [Bind("Id,Ad")] UzmanlikAlani alan)
    {
        if (id != alan.Id)
        {
            return BadRequest();
        }
        if (!await _db.UzmanlikAlanlari.AnyAsync(u => u.Id == id))
        {
            return NotFound();
        }

        alan.Ad = alan.Ad?.Trim() ?? string.Empty;
        if (await _db.UzmanlikAlanlari.AnyAsync(u => u.Ad == alan.Ad && u.Id != id))
        {
            ModelState.AddModelError(nameof(UzmanlikAlani.Ad), "Bu isimde bir uzmanlık alanı zaten var.");
        }
        if (!ModelState.IsValid)
        {
            return View(alan);
        }

        _db.UzmanlikAlanlari.Update(alan);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{alan.Ad}\" güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Sil(int id)
    {
        var alan = await _db.UzmanlikAlanlari
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UzmanlikAlaniListeOgesi
            {
                Id = u.Id,
                Ad = u.Ad,
                VeterinerSayisi = u.Veterinerler.Count
            })
            .FirstOrDefaultAsync();

        if (alan is null)
        {
            return NotFound();
        }
        return View(alan);
    }

    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnaylandi(int id)
    {
        var alan = await _db.UzmanlikAlanlari.FindAsync(id);
        if (alan is null)
        {
            return NotFound();
        }

        if (await _db.Veterinerler.AnyAsync(v => v.UzmanlikAlanlari.Any(u => u.Id == id)))
        {
            TempData["Hata"] = $"\"{alan.Ad}\" alanında çalışan veterinerler olduğu için silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _db.UzmanlikAlanlari.Remove(alan);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"\"{alan.Ad}\" silindi.";
        return RedirectToAction(nameof(Index));
    }
}