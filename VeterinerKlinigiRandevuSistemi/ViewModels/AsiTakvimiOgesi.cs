namespace VeterinerKlinigiRandevuSistemi.ViewModels;

public class AsiTakvimiOgesi
{
    public int EvcilHayvanId { get; set; }
    public string HayvanAdi { get; set; } = string.Empty;
    public string AsiAdi { get; set; } = string.Empty;
    public DateOnly SonYapilma { get; set; }
    public DateOnly SonrakiDoz { get; set; }
    public int KalanGun => SonrakiDoz.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
}