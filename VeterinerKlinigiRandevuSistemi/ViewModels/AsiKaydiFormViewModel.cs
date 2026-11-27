using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.ViewModels;

public class AsiKaydiFormViewModel : IValidatableObject
{
    public static readonly string[] YayginAsilar =
    {
        "Karma Aşı", "Kuduz", "Lösemi (FeLV)", "Bronşin (Kennel Cough)",
        "Lyme", "Corona", "İç Parazit", "Dış Parazit"
    };

    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }

    public string HayvanAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Aşı Adı")]
    public string AsiAdi { get; set; } = string.Empty;

    [Display(Name = "Yapıldığı Tarih")]
    [DataType(DataType.Date)]
    public DateOnly YapilmaTarihi { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Sonraki Doz Tarihi")]
    [DataType(DataType.Date)]
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);

        if (YapilmaTarihi > bugun)
        {
            yield return new ValidationResult("Yapıldığı tarih gelecekte olamaz.", new[] { nameof(YapilmaTarihi) });
        }
        if (SonrakiDozTarihi.HasValue && SonrakiDozTarihi <= YapilmaTarihi)
        {
            yield return new ValidationResult("Sonraki doz, yapıldığı tarihten sonra olmalıdır.", new[] { nameof(SonrakiDozTarihi) });
        }
    }
}