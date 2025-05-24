// MVC Projesi - Controllers/AssignmentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // IConfiguration için

public class AssignmentsController : Controller
{
    private readonly string _apiBaseUrl;
    private readonly IConfiguration _configuration;

    public AssignmentsController(IConfiguration configuration)
    {
        _configuration = configuration;
        _apiBaseUrl = _configuration.GetValue<string>("ApiBaseURL");
    }

    // Yeni ödev oluşturma formunu gösterecek GET action'ı
    // Örn: /Assignments/Create?courseId=123
    [HttpGet]
    public IActionResult Create([FromQuery] int courseId) // courseId'yi query string'den alır
    {
        if (courseId <= 0)
        {
            TempData["ErrorMessage"] = "Geçersiz Kurs ID.";
            return RedirectToAction("Index", "Courses"); // Veya uygun bir hata sayfasına
        }
        ViewBag.CourseId = courseId;
        ViewBag.ApiBaseURL = _apiBaseUrl;
        // Formun başlığında göstermek için kurs adını da API'den çekip ViewBag'e atabilirsiniz (isteğe bağlı)
        // Veya JavaScript API'den kurs adını çekip başlığa yazabilir.
        return View(); // Views/Assignments/Create.cshtml dosyasını arayacak
    }

}