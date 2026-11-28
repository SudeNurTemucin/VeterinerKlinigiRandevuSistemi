using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Yardimcilar;

// Randevu durumunu ekranda Türkçe metin ve renkli rozet olarak göstermek için
public static class DurumYardimcisi
{
    public static string Metin(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Bekliyor => "Onay bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        RandevuDurumu.IptalEdildi => "İptal edildi",
        _ => durum.ToString()
    };

    public static string RozetSinifi(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Bekliyor => "text-bg-warning",
        RandevuDurumu.Onaylandi => "text-bg-success",
        RandevuDurumu.Reddedildi => "text-bg-danger",
        RandevuDurumu.Tamamlandi => "text-bg-primary",
        _ => "text-bg-secondary"
    };
}