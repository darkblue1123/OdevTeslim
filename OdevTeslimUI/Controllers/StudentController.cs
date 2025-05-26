using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OdevTeslimUI.Controllers
{
    public class StudentController : Controller
    {
        private readonly string _apiBaseUrl;

        public StudentController(IConfiguration configuration)
        {
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseURL");
        }
        public IActionResult Index()
        {
            ViewBag.ApiBaseURL = _apiBaseUrl; 
            return View(); 
        }
        
        public IActionResult CourseAssignmentsPage(int id) 
        {
            if (id <= 0)
            {
              
                TempData["ErrorMessage"] = "Geçersiz kurs ID'si.";
                return RedirectToAction(nameof(Index)); 
            }

            ViewBag.CourseId = id;
            ViewBag.ApiBaseURL = _apiBaseUrl; 
            return View();
        }

    }
}
