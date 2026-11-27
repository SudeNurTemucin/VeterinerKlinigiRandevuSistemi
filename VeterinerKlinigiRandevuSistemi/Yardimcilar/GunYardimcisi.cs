using System.Globalization;

namespace VeterinerKlinigiRandevuSistemi.Yardimcilar;

//DayOfWeek .NET'te İngilizcedir vehaftaya Pazar'dan başladığı için, sıralama ve görüntüleme için yardımcı sınıf yazılmıştır.
public static class GunYardimcisi
{
    private static readonly CultureInfo Turkce = new("tr-TR");
    public static readonly DayOfWeek[] HaftaGunleri =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public static string TurkceAd(DayOfWeek gun) => Turkce.DateTimeFormat.GetDayName(gun);
    //Pazartesi=0,...
    public static int Sira(DayOfWeek gun) => ((int)gun + 6) % 7;
}