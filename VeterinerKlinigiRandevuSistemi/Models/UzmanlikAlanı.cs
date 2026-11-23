using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class UzmanlikAlani
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Uzmanlık adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Uzmanlık Alanı")]
    public string Ad { get; set; } = string.Empty;

    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
}