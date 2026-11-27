using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;
using VeterinerKlinigiRandevuSistemi.Yardimcilar;

namespace VeterinerKlinigiRandevuSistemi.Controllers;

//Idor Koruması: Bu controller'daki her sorgu KullanicininHayvanlari() üzerinden başlar.
// O metot "sadece giriş yapan kullanıcının hayvanları" filtresini ekler.
[Authorize(Roles = Roller.Uye)]
public class EvcilHayvanController : Controller
{
    private const long MaksimumFotografBoyutu = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] IzinVerilenUzantilar = { ".jpg", ".jpeg", ".png", ".webp" };
    private const string FotografUrlOnEki = YuklemeAyarlari.UrlOnEki + "/hayvanlar"; // "/yuklemeler/hayvanlar"

    private readonly UygulamaDbContext _db;
    private readonly UserManager<Kullanici> _kullaniciYoneticisi;

    public EvcilHayvanController(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        _db = db;
        _kullaniciYoneticisi = kullaniciYoneticisi;
    }

    private string KullaniciId => _kullaniciYoneticisi.GetUserId(User)!;

    // IDOR korumasının kalbi: bütün sorgular buradan başlar.
    private IQueryable<EvcilHayvan> KullanicininHayvanlari() =>
        _db.EvcilHayvanlar.Where(h => h.SahipId == KullaniciId);

    public async Task<IActionResult> Index()
    {
        var hayvanlar = await KullanicininHayvanlari()
            .AsNoTracking()
            .OrderBy(h => h.Ad)
            .ToListAsync();

        return View(hayvanlar);
    }

    public async Task<IActionResult> Detay(int id)
    {
        var hayvan = await KullanicininHayvanlari()
            .AsNoTracking()
            .Include(h => h.AsiKayitlari)          
            .FirstOrDefaultAsync(h => h.Id == id);

        // Hayvan yoksa veya başkasınınsa ikisi de NotFound (404) döner.
        // 403 dönsey "bu Id'de bir hayvan var ama senin değil" bilgisini sızdırır.
        if (hayvan is null)
        {
            return NotFound();
        }
        return View(hayvan);
    }

    
    public IActionResult Ekle()
    {
        return View(new EvcilHayvanFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(EvcilHayvanFormViewModel model)
    {
        FotografiDogrula(model.Fotograf);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hayvan = new EvcilHayvan
        {
            Ad = model.Ad.Trim(),
            Tur = model.Tur,
            Irk = model.Irk?.Trim(),
            DogumTarihi = model.DogumTarihi,
            Kilo = model.Kilo,
            SahipId = KullaniciId   //Sahip formdan değil, oturumdan gelir
        };

        if (model.Fotograf is not null)
        {
            hayvan.FotografYolu = await FotografiKaydetAsync(model.Fotograf);
        }

        _db.EvcilHayvanlar.Add(hayvan);
        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{hayvan.Ad} eklendi.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Duzenle(int id)
    {
        var hayvan = await KullanicininHayvanlari()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hayvan is null)
        {
            return NotFound();
        }

        var model = new EvcilHayvanFormViewModel
        {
            Id = hayvan.Id,
            Ad = hayvan.Ad,
            Tur = hayvan.Tur,
            Irk = hayvan.Irk,
            DogumTarihi = hayvan.DogumTarihi,
            Kilo = hayvan.Kilo,
            MevcutFotografYolu = hayvan.FotografYolu
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(int id, EvcilHayvanFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        // POST'ta da sahiplik kontrolü şart! get'teki kontrol tek başına yetmez,
        // çünkü form Postman gibi bir araçla doğrudan da gönderilebilir.
        var hayvan = await KullanicininHayvanlari().FirstOrDefaultAsync(h => h.Id == id);
        if (hayvan is null)
        {
            return NotFound();
        }

        FotografiDogrula(model.Fotograf);
        if (!ModelState.IsValid)
        {
            model.MevcutFotografYolu = hayvan.FotografYolu;
            return View(model);
        }

        hayvan.Ad = model.Ad.Trim();
        hayvan.Tur = model.Tur;
        hayvan.Irk = model.Irk?.Trim();
        hayvan.DogumTarihi = model.DogumTarihi;
        hayvan.Kilo = model.Kilo;

        if (model.Fotograf is not null)
        {
            FotografiSil(hayvan.FotografYolu);
            hayvan.FotografYolu = await FotografiKaydetAsync(model.Fotograf);
        }
        else if (model.FotografiKaldir)
        {
            FotografiSil(hayvan.FotografYolu);
            hayvan.FotografYolu = null;
        }

        await _db.SaveChangesAsync();

        TempData["Basari"] = $"{hayvan.Ad} güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }
    public async Task<IActionResult> Sil(int id)
    {
        var hayvan = await KullanicininHayvanlari()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hayvan is null)
        {
            return NotFound();
        }
        return View(hayvan);
    }

    [HttpPost, ActionName("Sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SilOnaylandi(int id)
    {
        var hayvan = await KullanicininHayvanlari().FirstOrDefaultAsync(h => h.Id == id);
        if (hayvan is null)
        {
            return NotFound();
        }

        // Randevu geçmişi olan hayvan silinmez.
        if (await _db.Randevular.AnyAsync(r => r.EvcilHayvanId == id))
        {
            TempData["Hata"] = $"{hayvan.Ad} için randevu kaydı olduğu için silinemez.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var fotografYolu = hayvan.FotografYolu;
        _db.EvcilHayvanlar.Remove(hayvan);   //Aşı kayıtları Cascade ile silinir
        await _db.SaveChangesAsync();
        FotografiSil(fotografYolu);

        TempData["Basari"] = $"{hayvan.Ad} silindi.";
        return RedirectToAction(nameof(Index));
    }

    private void FotografiDogrula(IFormFile? dosya)
    {
        if (dosya is null || dosya.Length == 0)
        {
            return;
        }

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!IzinVerilenUzantilar.Contains(uzanti))
        {
            ModelState.AddModelError(nameof(EvcilHayvanFormViewModel.Fotograf),
                "Sadece JPG, PNG veya WEBP dosyası yükleyebilirsiniz.");
        }
        if (dosya.Length > MaksimumFotografBoyutu)
        {
            ModelState.AddModelError(nameof(EvcilHayvanFormViewModel.Fotograf),
                "Fotoğraf en fazla 2 MB olabilir.");
        }
    }

    // Fotoğraflar proje klasörünün DIŞINDA: ...\AppData\Local\VeterinerKlinigiRandevuSistemi\Yuklemeler\hayvanlar
    // (F5 ile hata ayıklarken VS, proje klasörüne yazılan yeni dosyada uygulamayı kapatıyordu.)
    private static string FotografKlasoru => Path.Combine(YuklemeAyarlari.Klasor, "hayvanlar");

    private async Task<string> FotografiKaydetAsync(IFormFile dosya)
    {
        var klasor = FotografKlasoru;
        Directory.CreateDirectory(klasor); 

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        var dosyaAdi = $"{Guid.NewGuid()}{uzanti}";

        await using var akis = new FileStream(Path.Combine(klasor, dosyaAdi), FileMode.Create);
        await dosya.CopyToAsync(akis);

        return $"{FotografUrlOnEki}/{dosyaAdi}"; // Veritabanına yazılan yol: /yuklemeler/hayvanlar/xxx.jpg
    }

    private void FotografiSil(string? yol)
    {
        if (string.IsNullOrEmpty(yol))
        {
            return;
        }

        // Sadece kendi klasörümüzdeki dosyayı sil (güvenlik)
        var dosyaAdi = Path.GetFileName(yol);
        var tamYol = Path.Combine(FotografKlasoru, dosyaAdi);
        if (System.IO.File.Exists(tamYol))
        {
            System.IO.File.Delete(tamYol);
        }
    }
}