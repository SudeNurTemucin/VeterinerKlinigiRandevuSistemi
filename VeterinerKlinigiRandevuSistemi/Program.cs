using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.Yardimcilar;

var builder = WebApplication.CreateBuilder(args);

// ===== 1) SERVİSLER: Dependency Injection kabına kayıt =====
builder.Services.AddControllersWithViews(options =>                // Adım 1 (4B'de Türkçe mesajlar eklendi)
{
    // Model binding hata mesajlarını Türkçeleştir.
    // İstemci tarafındaki "geçerli bir sayı giriniz" kontrolü de bu mesajı kullanır.
    var mesajlar = options.ModelBindingMessageProvider;
    mesajlar.SetValueMustBeANumberAccessor(alan => $"{alan} alanına geçerli bir sayı giriniz.");
    mesajlar.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
    mesajlar.SetAttemptedValueIsInvalidAccessor((deger, alan) => $"'{deger}' değeri {alan} alanı için geçersiz.");
    mesajlar.SetValueIsInvalidAccessor(deger => $"'{deger}' değeri geçersiz.");
});

builder.Services.AddDbContext<UygulamaDbContext>(options =>       // Adım 2: EF Core + SQLite
    options.UseSqlite(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

builder.Services.AddIdentity<Kullanici, IdentityRole>(options =>  // Adım 3: Identity
{
    // Admin şifresi "sau" olduğu için varsayılan şifre kurallarını gevşetiyoruz
    options.Password.RequiredLength = 3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<UygulamaDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>            // Adım 3: yönlendirme adresleri
{
    options.LoginPath = "/Hesap/Giris";
    options.LogoutPath = "/Hesap/Cikis";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
});

var app = builder.Build();

// ===== 2) BAŞLANGIÇ VERİSİ: migration + roller + admin =====
using (var scope = app.Services.CreateScope())                    // Adım 3
{
    await BaslangicVerisi.YukleAsync(scope.ServiceProvider);
}

// ===== 3) MIDDLEWARE HATTI: her istek bu sırayla geçer =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

// Adım 6: Kullanıcıların yüklediği fotoğraflar proje klasörünün DIŞINDA tutulur
// (bkz. Yardimcilar/YuklemeAyarlari.cs). MapStaticAssets sadece derleme anında var olan
// dosyaları sunar; sonradan yüklenenler için klasik UseStaticFiles gerekir.
// "/yuklemeler/hayvanlar/abc.jpg" -> ...\AppData\Local\VeterinerKlinigiRandevuSistemi\Yuklemeler\hayvanlar\abc.jpg
Directory.CreateDirectory(YuklemeAyarlari.Klasor);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(YuklemeAyarlari.Klasor),
    RequestPath = YuklemeAyarlari.UrlOnEki
});

// Adım 4B: Uygulamanın kültürü Türkçe (ondalık ayıracı virgül, para birimi ₺).
// Hocanın bilgisayarı İngilizce olsa bile "150,50" doğru okunur.
var turkce = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkce),
    SupportedCultures = new List<CultureInfo> { turkce },
    SupportedUICultures = new List<CultureInfo> { turkce }
});

app.UseRouting();
app.UseAuthentication();   // Adım 3: "Sen kimsin?" (çerezi okur)
app.UseAuthorization();    //         "Buna yetkin var mı?"
app.MapStaticAssets();

// ===== 4) ROTALAR =====
app.MapControllerRoute(                                           // Adım 4A: Admin area
    name: "areas",
    pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(                                           // Adım 1: varsayılan rota
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
