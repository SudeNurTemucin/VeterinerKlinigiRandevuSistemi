using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigiRandevuSistemi.Models;

public class Kullanici : IdentityUser  //Identity'nin hazır kullanıcı sınıfını genişletir.
//E-posta, şifre özeti ve telefon gibi alanlar IdentityUser sınıfından miras gelir.
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50)]
    public string Soyad { get; set; } = string.Empty;

    public ICollection<EvcilHayvan> EvcilHayvanlar { get; set; } = new List<EvcilHayvan>();
}