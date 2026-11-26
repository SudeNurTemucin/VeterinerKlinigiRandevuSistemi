using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.ViewModels;

namespace VeterinerKlinigiRandevuSistemi.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roller.Admin)]
public class PanelController : Controller
{
    private readonly UygulamaDbContext _db;

    public PanelController(UygulamaDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var model = new PanelOzetViewModel
        {
            KlinikSayisi = await _db.Klinikler.CountAsync(),
            HizmetSayisi = await _db.Hizmetler.CountAsync(),
            VeterinerSayisi = await _db.Veterinerler.CountAsync(),
            BekleyenRandevuSayisi = await _db.Randevular
                .CountAsync(r => r.Durum == RandevuDurumu.Bekliyor)
        };
        return View(model);
    }
}
