using Microsoft.AspNetCore.Mvc;

namespace OdevTeslimUI.Controllers
{
    public class UserController : Controller
    {
        private readonly string _apiBaseUrl;

        public UserController(IConfiguration configuration)
        {
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseURL");
        }

        public IActionResult Index()
        {
            ViewBag.ApiBaseURL = _apiBaseUrl; // JavaScript'in API adresini bilmesi için
            return View();
        }
    }
}
