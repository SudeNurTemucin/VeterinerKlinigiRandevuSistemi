using Microsoft.EntityFrameworkCore;
using VeterinerKlinigiRandevuSistemi.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<UygulamaDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

var app = builder.Build();

// Uygulama açılırken bekleyen migration'ları uygula.Hiçbir komut satırı veya kullanıcı girişi gerektirmez, uygulama açılırken otomatik olarak yapılır.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
    db.Database.Migrate();
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
