using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VeterinerKlinigiRandevuSistemi.Data;
using VeterinerKlinigiRandevuSistemi.Models;
using VeterinerKlinigiRandevuSistemi.Services;
using VeterinerKlinigiRandevuSistemi.Yardimcilar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => 
{
    // Model binding hata mesajlarını Türkçeleştir.
    var mesajlar = options.ModelBindingMessageProvider;
    mesajlar.SetValueMustBeANumberAccessor(alan => $"{alan} alanına geçerli bir sayı giriniz.");
    mesajlar.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
    mesajlar.SetAttemptedValueIsInvalidAccessor((deger, alan) => $"'{deger}' değeri {alan} alanı için geçersiz.");
    mesajlar.SetValueIsInvalidAccessor(deger => $"'{deger}' değeri geçersiz.");
});

builder.Services.AddDbContext<UygulamaDbContext>(options =>       //EF Core + SQLite
    options.UseSqlite(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

builder.Services.AddIdentity<Kullanici, IdentityRole>(options =>  //Identity
{
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

//Randevu iş kuralları servisi
//Scoped: her HTTP isteği için bir tane oluşturulur, DbContext ile aynı ömür.
builder.Services.AddScoped<IRandevuServisi, RandevuServisi>();

builder.Services.ConfigureApplicationCookie(options => 
{
    options.LoginPath = "/Hesap/Giris";
    options.LogoutPath = "/Hesap/Cikis";
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi";
});

var app = builder.Build();

//Başlangıç verisi migration + roller + admin
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

Directory.CreateDirectory(YuklemeAyarlari.Klasor);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(YuklemeAyarlari.Klasor),
    RequestPath = YuklemeAyarlari.UrlOnEki   
});

var turkce = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkce),
    SupportedCultures = new List<CultureInfo> { turkce },
    SupportedUICultures = new List<CultureInfo> { turkce }
});

app.UseRouting();
app.UseAuthentication(); //"Sen kimsin?"
app.UseAuthorization();  //"Buna yetkin var mı?"
app.MapStaticAssets();

app.MapControllerRoute(  //Admin Area
    name: "areas",
    pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(                                     
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();