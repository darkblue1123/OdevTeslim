using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OdevTeslim.Models;
using OdevTeslim.Repositories;
using System.Reflection;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(p => p.AddPolicy("corspolicy", opt =>
{
    opt.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApiUyg",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


// ===> BU SATIRIN EKLENMESİ GEREKİR <===
// AppDbContext'i servis koleksiyonuna ekle ve SQL Server kullanacağını belirt (veya kullandığınız başka bir veritabanı sağlayıcısı: UseNpgsql, UseSqlite vb.)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlCon"))); // VEYA UseNpgsql(connectionString) VEYA UseSqlite(connectionString) vb.

// Identity servislerini eklerken DbContext'i belirtin

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
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireUppercase = true;


    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
    options.Lockout.MaxFailedAccessAttempts = 3;



})
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();


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
app.UseRouting(); // 1. Routing middleware'i (Endpoint'leri belirlemek için)

app.UseCors("corspolicy"); // 2. TANIMLADIĞINIZ CORS POLİTİKASINI UYGULAYIN!
app.UseAuthorization();

app.MapControllers();
app.MapControllers(); // 5. Controller endpoint'lerini eşle


app.Run();
