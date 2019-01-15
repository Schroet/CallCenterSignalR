using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}