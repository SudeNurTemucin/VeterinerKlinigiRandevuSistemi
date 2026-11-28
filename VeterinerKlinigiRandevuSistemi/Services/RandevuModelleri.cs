using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Services;
public record RandevuTalebi(
    string KullaniciId,
    int EvcilHayvanId,
    int VeterinerId,
    int HizmetId,
    DateTime Baslangic,
    string? Aciklama);

//Randevu oluşturma/iptal etme işlemlerinin sonucu
public record RandevuSonucu(bool Basarili, string Mesaj, int? RandevuId = null)
{
    public static RandevuSonucu Tamam(string mesaj, int? randevuId = null) => new(true, mesaj, randevuId);
    public static RandevuSonucu Hata(string mesaj) => new(false, mesaj);
}

//Randevu listesi için özet model
public class RandevuOzeti
{
    public int Id { get; set; }
    public string HayvanAdi { get; set; } = string.Empty;
    public string VeterinerAdi { get; set; } = string.Empty;
    public string KlinikAdi { get; set; } = string.Empty;
    public string HizmetAdi { get; set; } = string.Empty;
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public decimal Ucret { get; set; }
    public RandevuDurumu Durum { get; set; }
    public string? Aciklama { get; set; }

    // İptal butonu sadece bekleyen/onaylı ve henüz başlamamış randevularda görünür
    public bool IptalEdilebilir =>
        (Durum == RandevuDurumu.Bekliyor || Durum == RandevuDurumu.Onaylandi) && Baslangic > DateTime.Now;
}