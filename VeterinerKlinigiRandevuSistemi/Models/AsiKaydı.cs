using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class AsiKaydi
{
    public int Id { get; set; }

    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Aşı Adı")]
    public string AsiAdi { get; set; } = string.Empty;

    [Display(Name = "Yapıldığı Tarih")]
    [DataType(DataType.Date)]
    public DateOnly YapilmaTarihi { get; set; }

    [Display(Name = "Sonraki Doz")]
    [DataType(DataType.Date)]
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }
}