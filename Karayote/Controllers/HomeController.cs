using KarafunAPI;
using Karayote.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Karayote.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IKarafun _karafun;

        public HomeController(ILogger<HomeController> logger, IKarafun karafun)
        {
            _logger = logger;
            _karafun = karafun;
        }

        public IActionResult Index()
        {
            return View(_karafun.Status);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}