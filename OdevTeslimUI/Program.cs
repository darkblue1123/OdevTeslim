using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication() // Varsayılan şemayı belirtmeyebilirsiniz veya projenize göre ayarlayabilirsiniz
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login"; // MVC Login sayfanızın yolu
        options.AccessDeniedPath = "/Account/AccessDenied"; // MVC Yetkisiz Erişim sayfanızın yolu
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie geçerlilik süresi
        options.SlidingExpiration = true; // Kullanıcı aktif oldukça sürenin uzatılması
    });
    
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// MVC Projesi - Program.cs

// Eğer MVC projesi de Identity tablolarına erişip _signInManager kullanacaksa
// (örn: harici token'ı alıp kendi cookie'sini oluşturmak için),
// o zaman burada da AddIdentity<AppUser, AppRole> gerekebilir.
// Ama genellikle MVC projesi, API'den gelen token'ı doğrulatıp kendi oturumunu açar.
// Ya da daha basit senaryolarda, MVC projesinin kendi Identity yönetimi olmayabilir,
// sadece API'den gelen token'ı bir MVC action'ına gönderip o action'da HttpContext.SignInAsync ile cookie oluşturulur.

 app.UseAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();
