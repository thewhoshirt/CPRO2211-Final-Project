using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Music_App.Models;

namespace Music_App.Controllers
{
    public class HomeController : Controller
    {
        private MusicContext context { get; set; }

        public HomeController(MusicContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            var music = context.Musics.OrderBy(m => m.TrackId).ToList();
            return View(music);
        }
    }
}
