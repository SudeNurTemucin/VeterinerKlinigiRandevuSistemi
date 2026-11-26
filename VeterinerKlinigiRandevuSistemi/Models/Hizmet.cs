using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class Hizmet
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Hizmet Adı")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [Range(5, 480, ErrorMessage = "Süre 5 ile 480 dakika arasında olmalıdır.")]
    [Display(Name = "Süre (dk)")]
    public int SureDakika { get; set; }

    [Range(0, 100000, ErrorMessage = "Ücret 0 ile 100.000 arasında olmalıdır.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Ücret")]
    public decimal Ucret { get; set; }

    //Açılır listede seçim yapılmazsa Türkçe mesaj gösterir.
    [Required(ErrorMessage = "Lütfen bir klinik seçiniz.")]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}