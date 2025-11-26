using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Music_App.Models;

namespace Music_App.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
