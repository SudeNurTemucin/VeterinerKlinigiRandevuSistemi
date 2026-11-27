namespace VeterinerKlinigiRandevuSistemi.Yardimcilar;

public static class YasYardimcisi
{
    public static string YasMetni(DateOnly dogumTarihi)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);

        int toplamAy = (bugun.Year - dogumTarihi.Year) * 12 + (bugun.Month - dogumTarihi.Month);
        if (bugun.Day < dogumTarihi.Day)
        {
            toplamAy--; // Ayın günü henüz gelmediyse o ay tamamlanmamıştır
        }

        if (toplamAy < 1)
        {
            return "1 aydan küçük";
        }

        int yil = toplamAy / 12;
        int ay = toplamAy % 12;

        if (yil == 0) return $"{ay} aylık";
        if (ay == 0) return $"{yil} yaş";
        return $"{yil} yaş {ay} ay";
    }
}