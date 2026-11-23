using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class Veteriner
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50)]
    public string Soyad { get; set; } = string.Empty;

    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    // Veterinerin sisteme giriş yaptığı hesap (1-1 ilişki)
    public string? KullaniciId { get; set; }
    public Kullanici? Kullanici { get; set; }

    public ICollection<UzmanlikAlani> UzmanlikAlanlari { get; set; } = new List<UzmanlikAlani>();
    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<MusaitlikSaati> MusaitlikSaatleri { get; set; } = new List<MusaitlikSaati>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();

    [NotMapped]
    [Display(Name = "Veteriner")]
    public string TamAd => $"Dr. {Ad} {Soyad}";
}