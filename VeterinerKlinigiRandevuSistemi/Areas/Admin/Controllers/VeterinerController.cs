using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class VeterinerController : Controller
{
    private readonly UygulamaDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public VeterinerController(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    public async Task<IActionResult> Index(int? klinikId)
    {
        IQueryable<Veteriner> sorgu = _db.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.Kullanici)
            .Include(v => v.UzmanlikAlanlari);

        if (klinikId.HasValue)
        {
            sorgu = sorgu.Where(v => v.KlinikId == klinikId.Value);
        }

        var veterinerler = await sorgu
            .OrderBy(v => v.Klinik!.Ad)
            .ThenBy(v => v.Ad)
            .ThenBy(v => v.Soyad)
            .ToListAsync();

        var klinikler = await _db.Klinikler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => new { k.Id, k.Ad })
            .ToListAsync();
        ViewBag.Klinikler = new SelectList(klinikler, "Id", "Ad", klinikId);

        return View(veterinerler);
    }

    //müsaitlik saatleri de bu sayfada yönetilir.
    public async Task<IActionResult> Detay(int id)
    {
        var veteriner = await _db.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.Kullanici)
            .Include(v => v.UzmanlikAlanlari)
            .Include(v => v.Hizmetler)
            .Include(v => v.MusaitlikSaatleri)
            .AsSplitQuery()   // 3 koleksiyon var: tek dev Join yerine ayrı ayrı küçük sorgular
            .FirstOrDefaultAsync(v => v.Id == id);

        if (veteriner is null)
        {
            return NotFound();
        }
        return View(veteriner);
    }

    public async Task<IActionResult> Ekle(int? klinikId)
    {
        if (!await _db.Klinikler.AnyAsync())
        {
            TempData["Hata"] = "Veteriner eklemeden önce en az bir klinik eklemelisiniz.";
            return RedirectToAction("Ekle", "Klinik");
        }

        var model = new VeterinerFormViewModel { KlinikId = klinikId ?? 0 };
        await SecenekleriDoldurAsync(model);
        return View(model);
    }

    // Veteriner kaydı + giriş hesabı + Veteriner rolü tek işlem içinde oluşturulur.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(VeterinerFormViewModel model)
    {
        model.Id = 0; // Ekleme formunda Id gönderilse bile yok sayılır.

        if (string.IsNullOrWhiteSpace(model.Eposta))
        {
            ModelState.AddModelError(nameof(model.Eposta), "Giriş e-postası zorunludur.");
        }
        else if (await _kullaniciYoneticisi.FindByEmailAsync(model.Eposta) is not null)
        {
            ModelState.AddModelError(nameof(model.Eposta), "Bu e-posta adresi zaten kullanılıyor.");
        }
        if (string.IsNullOrWhiteSpace(model.Sifre))
        {
            ModelState.AddModelError(nameof(model.Sifre), "Geçici şifre zorunludur.");
        }
        await KlinigiDogrulaAsync(model.KlinikId);

        if (!ModelState.IsValid)
        {
            await SecenekleriDoldurAsync(model);
            return View(model);
        }

        // UserManager de aynı DbContext'i kullandığı için hepsi aynı transaction'a girer.
        await using var islem = await _db.Database.BeginTransactionAsync();

        var kullanici = new Kullanici
        {
            UserName = model.Eposta,
            Email = model.Eposta,
            EmailConfirmed = true,
            Ad = model.Ad,
            Soyad = model.Soyad
        };

        var sonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, model.Sifre!);
        if (sonuc.Succeeded)
        {
            sonuc = await _kullaniciYoneticisi.AddToRoleAsync(kullanici, Roller.Veteriner);
        }
        if (!sonuc.Succeeded)
        {
            // CommitAsync çağrılmadan çıkıldığı için transaction geri alınır.
            foreach (var hata in sonuc.Errors)
            {
                ModelState.AddModelError(string.Empty, hata.Description);
            }
            await SecenekleriDoldurAsync(model);
            return View(model);
        }

        var veteriner = new Veteriner
        {
            Ad = model.Ad,
            Soyad = model.Soyad,
            KlinikId = model.KlinikId,
            KullaniciId = kullanici.Id
        };
        await SecimleriUygulaAsync(veteriner, model);

        _db.Veterinerler.Add(veteriner);
        await _db.SaveChangesAsync();
        await islem.CommitAsync();

        TempData["Basari"] = $"{veteriner.TamAd} eklendi. Şimdi çalışma saatlerini ekleyebilirsiniz.";
        return RedirectToAction(nameof(Detay), new { id = veteriner.Id });
    }

    public async Task<IActionResult> Duzenle(int id)
    {
        var veteriner = await _db.Veterinerler
            .AsNoTracking()
            .Include(v => v.Kullanici)
            .Include(v => v.UzmanlikAlanlari)
            .Include(v => v.Hizmetler)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (veteriner is null)
        {
            return NotFound();
        }

        var model = new VeterinerFormViewModel
        {
            Id = veteriner.Id,
            Ad = veteriner.Ad,
            Soyad = veteriner.Soyad,
            KlinikId = veteriner.KlinikId,
            Eposta = veteriner.Kullanici?.Email,
            SeciliUzmanlikIdleri = veteriner.UzmanlikAlanlari.Select(u => u.Id).ToList(),
            SeciliHizmetIdleri = veteriner.Hizmetler.Select(h => h.Id).ToList()
        };

        await SecenekleriDoldurAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, VeterinerFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        //Takip edilen nesne olduğu için değişiklikleri EF kendisi algılar.
        var veteriner = await _db.Veterinerler
            .Include(v => v.Kullanici)
            .Include(v => v.UzmanlikAlanlari)
            .Include(v => v.Hizmetler)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (veteriner is null)
        {
            return NotFound();
        }

        await KlinigiDogrulaAsync(model.KlinikId);
        if (!ModelState.IsValid)
        {
            model.Eposta = veteriner.Kullanici?.Email;
            await SecenekleriDoldurAsync(model);
            return View(model);
        }

        veteriner.Ad = model.Ad;
        veteriner.Soyad = model.Soyad;
        veteriner.KlinikId = model.KlinikId;
        await SecimleriUygulaAsync(veteriner, model);

        if (veteriner.Kullanici is not null)
        {
            veteriner.Kullanici.Ad = model.Ad;
            veteriner.Kullanici.Soyad = model.Soyad;
        }

        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{veteriner.TamAd} güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    public async Task<IActionResult> Sil(int id)
    {
        var veteriner = await _db.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.Kullanici)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (veteriner is null)
        {
            return NotFound();
        }
        return View(veteriner);
    }

    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnaylandi(int id)
    {
        var veteriner = await _db.Veterinerler.FindAsync(id);
        if (veteriner is null)
        {
            return NotFound();
        }

        if (await _db.Randevular.AnyAsync(r => r.VeterinerId == id))
        {
            TempData["Hata"] = $"{veteriner.TamAd} adına randevular olduğu için silinemez.";
            return RedirectToAction(nameof(Index));
        }

        await using var islem = await _db.Database.BeginTransactionAsync();

        var kullaniciId = veteriner.KullaniciId;
        var tamAd = veteriner.TamAd;

        // Müsaitlik saatleri ve ara tablo kayıtları Cascade ile otomatik silinir.
        _db.Veterinerler.Remove(veteriner);
        await _db.SaveChangesAsync();

        if (kullaniciId is not null)
        {
            var kullanici = await _kullaniciYoneticisi.FindByIdAsync(kullaniciId);
            if (kullanici is not null)
            {
                await _kullaniciYoneticisi.DeleteAsync(kullanici);
            }
        }

        await islem.CommitAsync();

        TempData["Basari"] = $"{tamAd} ve giriş hesabı silindi.";
        return RedirectToAction(nameof(Index));
    }

    //Müsaitlik saatleri ekleme ve silme işlemleri Veteriner detay sayfasında yapılır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikEkle(
        [Bind("VeterinerId,Gun,BaslangicSaati,BitisSaati")] MusaitlikSaati yeni)
    {
        var veteriner = await _db.Veterinerler
            .Include(v => v.Klinik)
            .Include(v => v.MusaitlikSaatleri)
            .FirstOrDefaultAsync(v => v.Id == yeni.VeterinerId);

        if (veteriner is null)
        {
            return NotFound();
        }

        var hata = ModelState.IsValid
            ? MusaitlikHatasiniBul(veteriner, yeni)
            : "Lütfen gün ve saatleri eksiksiz doldurunuz.";

        if (hata is not null)
        {
            TempData["Hata"] = hata;
        }
        else
        {
            veteriner.MusaitlikSaatleri.Add(yeni);
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Çalışma saati eklendi.";
        }

        return RedirectToAction(nameof(Detay), new { id = veteriner.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikSil(int id)
    {
        var musaitlik = await _db.MusaitlikSaatleri.FindAsync(id);
        if (musaitlik is null)
        {
            return NotFound();
        }

        _db.MusaitlikSaatleri.Remove(musaitlik);
        await _db.SaveChangesAsync();

        TempData["Basari"] = "Çalışma saati silindi.";
        return RedirectToAction(nameof(Detay), new { id = musaitlik.VeterinerId });
    }

    private static string? MusaitlikHatasiniBul(Veteriner veteriner, MusaitlikSaati yeni)
    {
        if (!Enum.IsDefined(yeni.Gun))
        {
            return "Geçersiz gün seçimi.";
        }
        if (yeni.BitisSaati <= yeni.BaslangicSaati)
        {
            return "Bitiş saati başlangıç saatinden sonra olmalıdır.";
        }

        var klinik = veteriner.Klinik!;
        if (yeni.BaslangicSaati < klinik.AcilisSaati || yeni.BitisSaati > klinik.KapanisSaati)
        {
            return $"Saatler kliniğin çalışma saatleri ({klinik.AcilisSaati:HH:mm} - {klinik.KapanisSaati:HH:mm}) içinde olmalıdır.";
        }

        //Çakışma olmaması için:yeni.Baslangic < mevcut.Bitis  VE  mevcut.Baslangic < yeni.Bitis
        bool cakisiyor = veteriner.MusaitlikSaatleri.Any(m =>
            m.Gun == yeni.Gun &&
            yeni.BaslangicSaati < m.BitisSaati &&
            m.BaslangicSaati < yeni.BitisSaati);

        return cakisiyor
            ? "Bu gün için girilen saatler mevcut bir çalışma aralığıyla çakışıyor."
            : null;
    }

    private async Task SecenekleriDoldurAsync(VeterinerFormViewModel model)
    {
        var klinikler = await _db.Klinikler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => new { k.Id, k.Ad })
            .ToListAsync();
        model.Klinikler = klinikler
            .Select(k => new SelectListItem { Value = k.Id.ToString(), Text = k.Ad })
            .ToList();

        model.Uzmanliklar = await _db.UzmanlikAlanlari
            .AsNoTracking()
            .OrderBy(u => u.Ad)
            .Select(u => new SecimOgesi { Id = u.Id, Ad = u.Ad })
            .ToListAsync();

        // Tüm kliniklerin hizmetleri gelir; jQuery sadece seçili kliniğinkileri gösterir.
        var hizmetler = await _db.Hizmetler
            .AsNoTracking()
            .OrderBy(h => h.Ad)
            .Select(h => new { h.Id, h.Ad, h.SureDakika, h.KlinikId })
            .ToListAsync();
        model.Hizmetler = hizmetler
            .Select(h => new SecimOgesi { Id = h.Id, Ad = $"{h.Ad} ({h.SureDakika} dk)", KlinikId = h.KlinikId })
            .ToList();
    }

    private async Task SecimleriUygulaAsync(Veteriner veteriner, VeterinerFormViewModel model)
    {
        var uzmanliklar = await _db.UzmanlikAlanlari
            .Where(u => model.SeciliUzmanlikIdleri.Contains(u.Id))
            .ToListAsync();

        var hizmetler = await _db.Hizmetler
            .Where(h => model.SeciliHizmetIdleri.Contains(h.Id) && h.KlinikId == model.KlinikId)
            .ToListAsync();

        // EF, ara tablodan neyi silip neyi ekleyeceğini kendisi hesaplar.
        veteriner.UzmanlikAlanlari.Clear();
        foreach (var uzmanlik in uzmanliklar)
        {
            veteriner.UzmanlikAlanlari.Add(uzmanlik);
        }

        veteriner.Hizmetler.Clear();
        foreach (var hizmet in hizmetler)
        {
            veteriner.Hizmetler.Add(hizmet);
        }
    }

    private async Task KlinigiDogrulaAsync(int klinikId)
    {
        if (!await _db.Klinikler.AnyAsync(k => k.Id == klinikId))
        {
            ModelState.AddModelError(nameof(VeterinerFormViewModel.KlinikId), "Lütfen geçerli bir klinik seçiniz.");
        }
    }
}