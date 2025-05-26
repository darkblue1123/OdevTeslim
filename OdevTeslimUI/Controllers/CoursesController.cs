// MVC Projesi - Controllers/CoursesController.cs
using Microsoft.AspNetCore.Mvc;

public class CoursesController : Controller
{
    private readonly string _apiBaseUrl;

    public CoursesController(IConfiguration configuration)
    {
        _apiBaseUrl = configuration.GetValue<string>("ApiBaseURL");
    }

    public IActionResult Index()
    {
        ViewBag.ApiBaseURL = _apiBaseUrl; 
      
        return View();
    }

    public IActionResult Details(int id)
    {
        ViewBag.CourseId = id;
        ViewBag.ApiBaseURL = _apiBaseUrl;
        
        return View();
    }

    public IActionResult Assignments(int id)
    {
        ViewBag.CourseId = id;
        ViewBag.ApiBaseURL = _apiBaseUrl;
        return View();
    }
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.ApiBaseURL = _apiBaseUrl;
        return View();
    }

}

