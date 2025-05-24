// MVC Projesi - Controllers/CoursesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; // [Authorize] attribute'u için (sayfaya erişim için)
using System.Text.Json; // JsonSerializer için (veya Newtonsoft.Json kullanıyorsanız ilgili using)
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
public class CoursesController : Controller
{
    private readonly string _apiBaseUrl;

    public CoursesController(IConfiguration configuration)
    {
        _apiBaseUrl = configuration.GetValue<string>("ApiBaseURL");
    }

    public IActionResult Index()
    {
        ViewBag.ApiBaseURL = _apiBaseUrl; // Bu hala JavaScript için gerekli
        // ViewBag.UserRolesJson ARTIK BURADA AYARLANMIYOR
        return View();
    }

    public IActionResult Details(int id)
    {
        ViewBag.CourseId = id;
        ViewBag.ApiBaseURL = _apiBaseUrl;
        // ViewBag.UserRolesJson ARTIK BURADA AYARLANMIYOR
        return View();
    }

    public IActionResult Assignments(int id)
    {
        ViewBag.CourseId = id;
        ViewBag.ApiBaseURL = _apiBaseUrl;
        // ViewBag.UserRolesJson ARTIK BURADA AYARLANMIYOR
        return View();
    }
    [HttpGet]
    public IActionResult Create()
    {
        // Sadece View'i döndürürken API adresini JavaScript'in kullanması için ViewBag'e atıyoruz.
        ViewBag.ApiBaseURL = _apiBaseUrl;
        return View(); // Views/Courses/Create.cshtml dosyasını arayacak
    }

}

