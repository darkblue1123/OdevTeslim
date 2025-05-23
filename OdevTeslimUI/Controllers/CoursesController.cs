// MVC Projesi - Controllers/CoursesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // [Authorize] attribute'u için (sayfaya erişim için)

public class CoursesController : Controller
{
    private readonly IConfiguration _configuration;

    public CoursesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

  
    public IActionResult Index() 
    {
        ViewData["Title"] = "Ders Listesi";
        ViewBag.ApiBaseURL = _configuration["ApiBaseUrl"]; // appsettings.json'dan API adresini al

        // Oturum açmış kullanıcının rollerini JavaScript'e aktarmak için
        var userRoles = User.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();
        ViewBag.UserRolesJson = System.Text.Json.JsonSerializer.Serialize(userRoles);

        return View(); // Views/Courses/Index.cshtml dosyasını döndürecek
    }
}