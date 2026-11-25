using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
using Microsoft.AspNetCore.Identity;
using VeterinerKlinigiRandevuSistemi.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<UygulamaDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

builder.Services.AddIdentity<Kullanici, IdentityRole>(options =>
{
    // Admin şifresi "sau" olduğu için varsayılan şifre kurallarını esnetilir.
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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
