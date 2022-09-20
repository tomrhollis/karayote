using Karayote.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Karayote.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISongQueue _queue;

        public HomeController(ILogger<HomeController> logger, ISongQueue q)
        {
            _logger = logger;
            _queue = q;
        }

        public IActionResult Index()
        {
            return View(new HomeViewModel(_queue.Status));
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