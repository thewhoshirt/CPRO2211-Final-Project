using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
//using MusicApp.Models;

namespace MusicApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
