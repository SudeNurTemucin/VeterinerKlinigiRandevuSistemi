using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Services;

public class RandevuServisi : IRandevuServisi
{
    public const int SaatAraligiDakika = 30;     //Müsait saatler 30 dakikada bir listelenir
    public const int EnFazlaGunSonrasi = 60;     //En fazla 60 gün sonrasına randevu alınabilir

    private readonly UygulamaDbContext _db;

    public RandevuServisi(UygulamaDbContext db)
    {
        _db = db;
    }

    //Müsait Saatler
    public async Task<List<TimeOnly>> MusaitSaatleriGetirAsync(int veterinerId, int hizmetId, DateOnly tarih)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Now);
        if (tarih < bugun || tarih > bugun.AddDays(EnFazlaGunSonrasi))
        {
            return new List<TimeOnly>();
        }

        // Veteriner bu hizmeti veriyor mu?
        var hizmet = await _db.Hizmetler
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hizmetId && h.Veterinerler.Any(v => v.Id == veterinerId));
        if (hizmet is null)
        {
            return new List<TimeOnly>();
        }

        // O günün çalışma aralıkları
        var calismaAraliklari = await _db.MusaitlikSaatleri
            .AsNoTracking()
            .Where(m => m.VeterinerId == veterinerId && m.Gun == tarih.DayOfWeek)
            .ToListAsync();
        if (calismaAraliklari.Count == 0)
        {
            return new List<TimeOnly>();
        }

        // O gün veterinerin dolu  randevuları
        var gunBaslangici = tarih.ToDateTime(TimeOnly.MinValue);
        var gunSonu = gunBaslangici.AddDays(1);
        var doluAraliklar = await _db.Randevular
            .AsNoTracking()
            .Where(r => r.VeterinerId == veterinerId
                     && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
                     && r.BaslangicZamani < gunSonu
                     && r.BitisZamani > gunBaslangici)
            .Select(r => new { r.BaslangicZamani, r.BitisZamani })
            .ToListAsync();

        var sure = TimeSpan.FromMinutes(hizmet.SureDakika);
        var simdi = DateTime.Now;
        var musaitSaatler = new List<TimeOnly>();

        foreach (var aralik in calismaAraliklari.OrderBy(a => a.BaslangicSaati))
        {
            var aralikSonu = tarih.ToDateTime(aralik.BitisSaati);

            // Aralığın başından itibaren 30'ar dakika ilerle; hizmet aralık bitmeden sığmalı
            for (var aday = tarih.ToDateTime(aralik.BaslangicSaati); aday + sure <= aralikSonu; aday = aday.AddMinutes(SaatAraligiDakika))
            {
                if (aday <= simdi)
                {
                    continue; //Geçmiş saat
                }

                var adayBitis = aday + sure;

                //Çakışma Formülü: A.Bas < B.Bit ve B.Bas < A.Bit
                bool dolu = doluAraliklar.Any(r => aday < r.BitisZamani && r.BaslangicZamani < adayBitis);
                if (!dolu)
                {
                    musaitSaatler.Add(TimeOnly.FromDateTime(aday));
                }
            }
        }

        return musaitSaatler;
    }

    //Randevu Oluşturma
    public async Task<RandevuSonucu> RandevuOlusturAsync(RandevuTalebi talep)
    {
        //Hayvan bu kullanıcının mı?
        var hayvan = await _db.EvcilHayvanlar
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == talep.EvcilHayvanId && h.SahipId == talep.KullaniciId);
        if (hayvan is null)
        {
            return RandevuSonucu.Hata("Evcil hayvan bulunamadı.");
        }

        //Veteriner bu hizmeti veriyor mu?
        var hizmet = await _db.Hizmetler
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == talep.HizmetId && h.Veterinerler.Any(v => v.Id == talep.VeterinerId));
        if (hizmet is null)
        {
            return RandevuSonucu.Hata("Seçilen veteriner bu hizmeti vermiyor.");
        }

        var baslangic = talep.Baslangic;
        var bitis = baslangic.AddMinutes(hizmet.SureDakika);

        //Zaman kuralları
        if (baslangic <= DateTime.Now)
        {
            return RandevuSonucu.Hata("Geçmiş bir zamana randevu alınamaz.");
        }
        if (baslangic.Date > DateTime.Today.AddDays(EnFazlaGunSonrasi))
        {
            return RandevuSonucu.Hata($"En fazla {EnFazlaGunSonrasi} gün sonrasına randevu alınabilir.");
        }

        // 4) Veterinerin o günkü çalışma saatleri içinde mi?
        var basSaat = TimeOnly.FromDateTime(baslangic);
        var bitSaat = TimeOnly.FromDateTime(bitis);
        var gununAraliklari = await _db.MusaitlikSaatleri
            .AsNoTracking()
            .Where(m => m.VeterinerId == talep.VeterinerId && m.Gun == baslangic.DayOfWeek)
            .ToListAsync();
        bool mesaide = bitis.Date == baslangic.Date &&
                       gununAraliklari.Any(m => m.BaslangicSaati <= basSaat && bitSaat <= m.BitisSaati);
        if (!mesaide)
        {
            return RandevuSonucu.Hata("Seçilen saat veterinerin çalışma saatleri dışında.");
        }

        // 5) Çakışma kontrolü + kayıt tek transaction içinde.
        // Aynı anda iki kişi aynı saate basarsa ikincisi kontrolde yakalanır.
        await using var islem = await _db.Database.BeginTransactionAsync();

        bool veterinerDolu = await _db.Randevular.AnyAsync(r =>
            r.VeterinerId == talep.VeterinerId
            && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
            && r.BaslangicZamani < bitis
            && baslangic < r.BitisZamani);
        if (veterinerDolu)
        {
            return RandevuSonucu.Hata("Bu saat dolu. Lütfen başka bir saat seçin.");
        }

        bool hayvaninBaskaRandevusuVar = await _db.Randevular.AnyAsync(r =>
            r.EvcilHayvanId == talep.EvcilHayvanId
            && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
            && r.BaslangicZamani < bitis
            && baslangic < r.BitisZamani);
        if (hayvaninBaskaRandevusuVar)
        {
            return RandevuSonucu.Hata($"{hayvan.Ad} için bu saatlerde başka bir randevu var.");
        }

        var randevu = new Randevu
        {
            EvcilHayvanId = hayvan.Id,
            VeterinerId = talep.VeterinerId,
            HizmetId = hizmet.Id,
            BaslangicZamani = baslangic,
            BitisZamani = bitis,
            Ucret = hizmet.Ucret, //Hizmete zam gelse de bu randevu değişmez
            Durum = RandevuDurumu.Bekliyor,
            Aciklama = string.IsNullOrWhiteSpace(talep.Aciklama) ? null : talep.Aciklama.Trim()
        };

        _db.Randevular.Add(randevu);
        await _db.SaveChangesAsync();
        await islem.CommitAsync();

        return RandevuSonucu.Tamam(
            $"{hayvan.Ad} için {baslangic:dd.MM.yyyy HH:mm} randevusu oluşturuldu. Veteriner onayı bekleniyor.",
            randevu.Id);
    }

    //Randevu İptal Etme
    public async Task<RandevuSonucu> RandevuIptalEtAsync(string kullaniciId, int randevuId)
    {
        // Sahiplik: randevu -> hayvan -> sahip
        var randevu = await _db.Randevular
            .FirstOrDefaultAsync(r => r.Id == randevuId && r.EvcilHayvan!.SahipId == kullaniciId);
        if (randevu is null)
        {
            return RandevuSonucu.Hata("Randevu bulunamadı.");
        }

        if (randevu.Durum != RandevuDurumu.Bekliyor && randevu.Durum != RandevuDurumu.Onaylandi)
        {
            return RandevuSonucu.Hata("Sadece bekleyen veya onaylanmış randevular iptal edilebilir.");
        }
        if (randevu.BaslangicZamani <= DateTime.Now)
        {
            return RandevuSonucu.Hata("Başlamış veya geçmiş bir randevu iptal edilemez.");
        }

        randevu.Durum = RandevuDurumu.IptalEdildi;
        await _db.SaveChangesAsync();

        return RandevuSonucu.Tamam("Randevu iptal edildi.", randevu.Id);
    }

    //Listeleme: Kullanıcının Randevuları
    public async Task<List<RandevuOzeti>> KullanicininRandevulariAsync(string kullaniciId)
    {
        // Projeksiyon: 4 tabloyu (Randevu, EvcilHayvan, Veteriner, Klinik, Hizmet) join'leyip sadece ekranda lazım olan kolonları çeker.
        return await _db.Randevular
            .AsNoTracking()
            .Where(r => r.EvcilHayvan!.SahipId == kullaniciId)
            .OrderByDescending(r => r.BaslangicZamani)
            .Select(r => new RandevuOzeti
            {
                Id = r.Id,
                HayvanAdi = r.EvcilHayvan!.Ad,
                VeterinerAdi = "Dr. " + r.Veteriner!.Ad + " " + r.Veteriner.Soyad,
                KlinikAdi = r.Veteriner.Klinik!.Ad,
                HizmetAdi = r.Hizmet!.Ad,
                Baslangic = r.BaslangicZamani,
                Bitis = r.BitisZamani,
                Ucret = r.Ucret,
                Durum = r.Durum,
                Aciklama = r.Aciklama
            })
            .ToListAsync();
    }
}