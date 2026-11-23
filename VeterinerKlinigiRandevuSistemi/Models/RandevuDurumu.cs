using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public enum RandevuDurumu  //Bu enum, randevuların durumlarını temsil eder.
{
    Bekliyor = 0,
    [Display(Name = "Onaylandı")] Onaylandi = 1,
    Reddedildi = 2,
    [Display(Name = "Tamamlandı")] Tamamlandi = 3,
    [Display(Name = "İptal Edildi")] IptalEdildi = 4
}