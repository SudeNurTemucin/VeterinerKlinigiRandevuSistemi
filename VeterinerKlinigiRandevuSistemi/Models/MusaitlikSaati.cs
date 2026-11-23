using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class MusaitlikSaati
{
    public int Id { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    [Display(Name = "Gün")]
    public DayOfWeek Gun { get; set; }

    [Display(Name = "Başlangıç")]
    [DataType(DataType.Time)]
    public TimeOnly BaslangicSaati { get; set; }

    [Display(Name = "Bitiş")]
    [DataType(DataType.Time)]
    public TimeOnly BitisSaati { get; set; }
}