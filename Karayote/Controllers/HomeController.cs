using KarafunAPI;
using Karayote.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

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
            return View(new HomeViewModel(_karafun.Status));
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

        [HttpGet]
        public JsonResult StatusUpdate()
        {
            return new JsonResult(_karafun.Status);
        }
    }
}