using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class Randevu
{
    public int Id { get; set; }

    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Display(Name = "Veteriner")]
    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    [Display(Name = "Hizmet")]
    public int HizmetId { get; set; }
    public Hizmet? Hizmet { get; set; }

    [Display(Name = "Başlangıç")]
    public DateTime BaslangicZamani { get; set; }

    // Hizmet süresinden hesaplanır; çakışma kontrolünü kolaylaştırır
    [Display(Name = "Bitiş")]
    public DateTime BitisZamani { get; set; }

    // Randevu anındaki ücret hizmet fiyatı sonradan değişse bile korunur.
    [DataType(DataType.Currency)]
    [Display(Name = "Ücret")]
    public decimal Ucret { get; set; }

    public RandevuDurumu Durum { get; set; } = RandevuDurumu.Bekliyor;

    [StringLength(500)]
    [Display(Name = "Sahip Notu")]
    public string? Aciklama { get; set; }

    public DateTime OlusturulmaZamani { get; set; } = DateTime.Now;

    public MuayeneNotu? MuayeneNotu { get; set; }
}