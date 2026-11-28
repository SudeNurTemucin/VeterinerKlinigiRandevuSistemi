namespace VeterinerKlinigiRandevuSistemi.Services;

// Controller'lar (MVC), API controller'ları ve MCP araçları kuralları kendileri yazmaz, bu servisi çağırır.
public interface IRandevuServisi
{
    // Bir veterinerin, bir hizmet için, belirli bir gündeki boş başlangıç saatleri
    Task<List<TimeOnly>> MusaitSaatleriGetirAsync(int veterinerId, int hizmetId, DateOnly tarih);

    Task<RandevuSonucu> RandevuOlusturAsync(RandevuTalebi talep);

    Task<RandevuSonucu> RandevuIptalEtAsync(string kullaniciId, int randevuId);

    Task<List<RandevuOzeti>> KullanicininRandevulariAsync(string kullaniciId);
}