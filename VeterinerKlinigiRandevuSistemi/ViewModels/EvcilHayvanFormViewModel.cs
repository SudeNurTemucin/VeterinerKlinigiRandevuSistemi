using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.ViewModels;

//Burada SahipId alanı kullanmıyoruz. Sahip her zaman giriş yapan kullanıcıdan atanır.
// Böylece kimse formu değiştirip hayvanı başkasının adına kaydedemez.
public class EvcilHayvanFormViewModel : IValidatableObject
{
    public static readonly string[] Turler =
        { "Kedi", "Köpek", "Kuş", "Tavşan", "Hamster", "Balık", "Sürüngen", "Diğer" };

    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür seçiniz.")]
    [StringLength(30)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Irk")]
    public string? Irk { get; set; }

    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateOnly DogumTarihi { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));

    [Range(0.01, 500, ErrorMessage = "Kilo 0,01 ile 500 kg arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public decimal Kilo { get; set; }

    // Yüklenen dosya
    [Display(Name = "Fotoğraf")]
    public IFormFile? Fotograf { get; set; }

    // Düzenlemede mevcut fotoğrafı göstermek ve kaldırmak için
    public string? MevcutFotografYolu { get; set; }

    [Display(Name = "Mevcut fotoğrafı kaldır")]
    public bool FotografiKaldir { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);

        if (DogumTarihi > bugun)
        {
            yield return new ValidationResult("Doğum tarihi gelecekte olamaz.", new[] { nameof(DogumTarihi) });
        }
        if (DogumTarihi < bugun.AddYears(-50))
        {
            yield return new ValidationResult("Doğum tarihi 50 yıldan eski olamaz.", new[] { nameof(DogumTarihi) });
        }
        if (!Turler.Contains(Tur))
        {
            yield return new ValidationResult("Listeden bir tür seçiniz.", new[] { nameof(Tur) });
        }
    }
}