using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeterinerKlinigiRandevuSistemi.ViewModels;

// Veteriner ekleme/düzenleme formudur ve Entity yerine ViewModel kullanılır çünkü formda veritabanında..
// Veteriner tablosunda olmayan alanlar var.
public class VeterinerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50)]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen bir klinik seçiniz.")]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }

    // Sadece yeni kayıtta zorunludur, kontrol controller'da yapılır.
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "Giriş E-postası")]
    public string? Eposta { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Şifre en az 3 karakter olmalıdır.")]
    [Display(Name = "Geçici Şifre")]
    public string? Sifre { get; set; }

    public List<int> SeciliUzmanlikIdleri { get; set; } = new();
    public List<int> SeciliHizmetIdleri { get; set; } = new();

    // Sadece ekranda göstermek içindir, formdan gelmez, doğrulanmaz.
    [ValidateNever]
    public List<SelectListItem> Klinikler { get; set; } = new();

    [ValidateNever]
    public List<SecimOgesi> Uzmanliklar { get; set; } = new();

    [ValidateNever]
    public List<SecimOgesi> Hizmetler { get; set; } = new();

    public bool YeniKayit => Id == 0;
}

public class SecimOgesi
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int? KlinikId { get; set; }   //Hizmetler için: hangi kliniğe ait
}