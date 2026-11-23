using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class EvcilHayvan
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(30)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Irk")]
    public string? Irk { get; set; }

    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateOnly DogumTarihi { get; set; }

    [Range(0.01, 500, ErrorMessage = "Kilo 0,01 ile 500 kg arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public decimal Kilo { get; set; }

    [StringLength(260)]
    public string? FotografYolu { get; set; }

    // Sahip formdan gelmez, giriş yapan kullanıcıdan atanır.
    [ValidateNever]
    public string SahipId { get; set; } = string.Empty;
    public Kullanici? Sahip { get; set; }

    public ICollection<AsiKaydi> AsiKayitlari { get; set; } = new List<AsiKaydi>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}