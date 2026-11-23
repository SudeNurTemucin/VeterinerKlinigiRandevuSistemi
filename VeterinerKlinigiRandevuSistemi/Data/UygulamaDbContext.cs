using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Models;

namespace VeterinerKlinigiRandevuSistemi.Data;

public class UygulamaDbContext : IdentityDbContext<Kullanici>
{
    public UygulamaDbContext(DbContextOptions<UygulamaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Klinik> Klinikler => Set<Klinik>();
    public DbSet<Hizmet> Hizmetler => Set<Hizmet>();
    public DbSet<UzmanlikAlani> UzmanlikAlanlari => Set<UzmanlikAlani>();
    public DbSet<Veteriner> Veterinerler => Set<Veteriner>();
    public DbSet<MusaitlikSaati> MusaitlikSaatleri => Set<MusaitlikSaati>();
    public DbSet<EvcilHayvan> EvcilHayvanlar => Set<EvcilHayvan>();
    public DbSet<AsiKaydi> AsiKayitlari => Set<AsiKaydi>();
    public DbSet<Randevu> Randevular => Set<Randevu>();
    public DbSet<MuayeneNotu> MuayeneNotlari => Set<MuayeneNotu>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity tablolarının yapılandırması için gerekli olan çağrı
        base.OnModelCreating(builder);

        // Veteriner <-> Kullanici (1-1): hesap silinirse veteriner kaydı kalır
        builder.Entity<Veteriner>()
            .HasOne(v => v.Kullanici)
            .WithOne()
            .HasForeignKey<Veteriner>(v => v.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);

        // Çoka-çok ilişkiler: ara tablolara anlamlı Türkçe isimler
        builder.Entity<Veteriner>()
            .HasMany(v => v.Hizmetler)
            .WithMany(h => h.Veterinerler)
            .UsingEntity(j => j.ToTable("VeterinerHizmetleri"));

        builder.Entity<Veteriner>()
            .HasMany(v => v.UzmanlikAlanlari)
            .WithMany(u => u.Veterinerler)
            .UsingEntity(j => j.ToTable("VeterinerUzmanliklari"));

        // Randevu <-> MuayeneNotu (1-1)
        builder.Entity<MuayeneNotu>()
            .HasOne(m => m.Randevu)
            .WithOne(r => r.MuayeneNotu)
            .HasForeignKey<MuayeneNotu>(m => m.RandevuId);

        // Klinik silinince hizmetleri ve hekimleri sessizce silinmesin
        builder.Entity<Hizmet>()
            .HasOne(h => h.Klinik)
            .WithMany(k => k.Hizmetler)
            .HasForeignKey(h => h.KlinikId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Veteriner>()
            .HasOne(v => v.Klinik)
            .WithMany(k => k.Veterinerler)
            .HasForeignKey(v => v.KlinikId)
            .OnDelete(DeleteBehavior.Restrict);

        // Randevusu olan hekim veya hizmet silinemesin (geçmiş korunur)
        builder.Entity<Randevu>()
            .HasOne(r => r.Veteriner)
            .WithMany(v => v.Randevular)
            .HasForeignKey(r => r.VeterinerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Randevu>()
            .HasOne(r => r.Hizmet)
            .WithMany(h => h.Randevular)
            .HasForeignKey(r => r.HizmetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Çakışma sorgusu "bu hekimin bu saatlerdeki randevuları" diye arayacak sadece, bu yüzden VeterinerId ve BaslangicZamani üzerinde index oluşturuyoruz.
        builder.Entity<Randevu>()
            .HasIndex(r => new { r.VeterinerId, r.BaslangicZamani });
    }
}