using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OdevTeslim.Models;
using OdevTeslim.Repositories;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ===> BU SATIRIN EKLENMESİ GEREKİR <===
// AppDbContext'i servis koleksiyonuna ekle ve SQL Server kullanacağını belirt (veya kullandığınız başka bir veritabanı sağlayıcısı: UseNpgsql, UseSqlite vb.)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlCon"))); // VEYA UseNpgsql(connectionString) VEYA UseSqlite(connectionString) vb.

// Identity servislerini eklerken DbContext'i belirtin
builder.Services.AddIdentity<AppUser, AppRole>(options => { /* Identity seçenekleri */ })
    .AddEntityFrameworkStores<AppDbContext>(); // <== Identity'nin hangi DbContext'i kullanacağını belirtin

// ===> Generic Repository Kaydı <===
// IGenericRepository<T> istendiğinde GenericRepository<T> örneği ver.
// AddScoped: İstek başına bir örnek oluşturulur.
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Specific Repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly()); // Veya profillerinizi içeren assembly'i belirtin


var app = builder.Build();


using (var scope = app.Services.CreateScope()) // Servislere erişim için scope oluştur
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>(); // AppRole kullandığımız için RoleManager<AppRole>

        string[] roleNames = { "Admin", "Teacher", "Student" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                // Rolü oluştur (AppRole sınıfını kullanarak)
                roleResult = await roleManager.CreateAsync(new AppRole { Name = roleName });
                if (!roleResult.Succeeded)
                {
                    // Hata loglanabilir
                    var logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError($"Error creating role {roleName}!");
                }
            }
        }

        // Opsiyonel: İlk Admin kullanıcısını burada oluşturabilirsiniz
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var adminEmail = "admin@yourapp.com"; // Kendi admin e-postanızı yazın
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var newAdminUser = new AppUser
            {
                UserName = "admin", // Kendi admin kullanıcı adınızı yazın
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true // Direkt onaylı yapalım
            };
            var createAdminResult = await userManager.CreateAsync(newAdminUser, "AdminPassword123!"); // GÜÇLÜ BİR ŞİFRE KULLANIN!
            if (createAdminResult.Succeeded)
            {
                // Admin kullanıcısını Admin rolüne ata
                await userManager.AddToRoleAsync(newAdminUser, "Admin");
            }
            else
            {
                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError($"Error creating admin user!");
                foreach (var error in createAdminResult.Errors) logger.LogError(error.Description);
            }
        }


    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred while seeding the database roles.");
    }
}
// === ROL OLUŞTURMA SONU ===




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var sqlCon = builder.Configuration.GetConnectionString("DefaultConnection");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
