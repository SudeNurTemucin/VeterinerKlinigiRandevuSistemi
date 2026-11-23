using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class MuayeneNotu
{
    public int Id { get; set; }

    public int RandevuId { get; set; }
    public Randevu? Randevu { get; set; }

    [Required(ErrorMessage = "Teşhis zorunludur.")]
    [StringLength(500)]
    [Display(Name = "Teşhis")]
    public string Teshis { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Tedavi { get; set; }

    [StringLength(1000)]
    public string? Notlar { get; set; }

    public DateTime OlusturulmaZamani { get; set; } = DateTime.Now;
}