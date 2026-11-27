namespace VeterinerKlinigiRandevuSistemi.Yardimcilar;

public static class YuklemeAyarlari
{
    public static readonly string Klasor = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VeterinerKlinigiRandevuSistemi",
        "Yuklemeler");
    public const string UrlOnEki = "/yuklemeler";
}