using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OdevTeslimUI.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "Admin")] // Veya action seviyesinde
        public IActionResult UserList() // Veya Index, Users vb.
        {
            return View(); // UserList.cshtml (veya verdiğiniz isim)
        }
    }
}
