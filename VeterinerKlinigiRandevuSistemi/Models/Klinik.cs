using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class Klinik : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Klinik adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Klinik Adı")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres zorunludur.")]
    [StringLength(250)]
    public string Adres { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon giriniz.")]
    [StringLength(20)]
    public string? Telefon { get; set; }

    [Display(Name = "Açılış Saati")]
    [DataType(DataType.Time)]
    public TimeOnly AcilisSaati { get; set; } = new(9, 0);

    [Display(Name = "Kapanış Saati")]
    [DataType(DataType.Time)]
    public TimeOnly KapanisSaati { get; set; } = new(18, 0);

    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();

    // İki alanı karşılaştıran iş kuralı,sadece sunucu tarafında çalışır.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (KapanisSaati <= AcilisSaati)
        {
            yield return new ValidationResult(
                "Kapanış saati açılış saatinden sonra olmalıdır.",
                new[] { nameof(KapanisSaati) });
        }
    }
}