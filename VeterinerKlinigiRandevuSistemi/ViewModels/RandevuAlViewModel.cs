using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeterinerKlinigiRandevuSistemi.ViewModels;

public class RandevuAlViewModel
{
    [Required(ErrorMessage = "Evcil hayvan seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Evcil hayvan seçiniz.")]
    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }

    [Required(ErrorMessage = "Klinik seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Klinik seçiniz.")]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }

    [Required(ErrorMessage = "Hizmet seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Hizmet seçiniz.")]
    [Display(Name = "Hizmet")]
    public int HizmetId { get; set; }

    [Required(ErrorMessage = "Veteriner seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Veteriner seçiniz.")]
    [Display(Name = "Veteriner")]
    public int VeterinerId { get; set; }

    [Required(ErrorMessage = "Tarih seçiniz.")]
    [Display(Name = "Tarih")]
    [DataType(DataType.Date)]
    public DateOnly Tarih { get; set; }

    [Required(ErrorMessage = "Saat seçiniz.")]
    [Display(Name = "Saat")]
    public TimeOnly? Saat { get; set; }

    [StringLength(500)]
    [Display(Name = "Açıklama (şikâyet, not)")]
    public string? Aciklama { get; set; }

    // Açılır listeler
    [ValidateNever]
    public List<SelectListItem> Hayvanlar { get; set; } = new();

    [ValidateNever]
    public List<SelectListItem> Klinikler { get; set; } = new();
}