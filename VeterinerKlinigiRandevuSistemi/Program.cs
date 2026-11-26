using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using VeterinerKlinigiRandevuSistemi.Data;
using Microsoft.AspNetCore.Identity;
using VeterinerKlinigiRandevuSistemi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    var mesajlar = options.ModelBindingMessageProvider;
    mesajlar.SetValueMustBeANumberAccessor(alan => $"{alan} alanına geçerli bir sayı giriniz.");
    mesajlar.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
    mesajlar.SetAttemptedValueIsInvalidAccessor((deger, alan) => $"'{deger}' değeri {alan} alanı için geçersiz.");
    mesajlar.SetValueIsInvalidAccessor(deger => $"'{deger}' değeri geçersiz.");
});

builder.Services.AddDbContext<UygulamaDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

builder.Services.AddIdentity<Kullanici, IdentityRole>(options =>
{
    //Varsayılan şifre kuralları esnetilir.
    options.Password.RequiredLength = 3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<UygulamaDbContext>()  //IdIdentity'ye kullanıcıları ve rolleri bizim veritabanımızda saklamasını söyler.
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => //Identity'nin cookie ayarlarını yapılandırır.
{
    options.LoginPath = "/Hesap/Giris";
    options.LogoutPath = "/Hesap/Cikis";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
});

var app = builder.Build();

// Uygulama başlatıldığında başlangıç verilerini yükle.
using (var scope = app.Services.CreateScope())
{
    await BaslangicVerisi.YukleAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var turkce = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkce),
    SupportedCultures = new List<CultureInfo> { turkce },
    SupportedUICultures = new List<CultureInfo> { turkce }
});


app.UseRouting();

app.UseAuthentication(); //Kullanıcının kimliğini doğrular, yani giriş yapmış mı kontrol eder.
app.UseAuthorization(); //Kişinin girme yetkisi olup olmadığını kontrol eder.

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
