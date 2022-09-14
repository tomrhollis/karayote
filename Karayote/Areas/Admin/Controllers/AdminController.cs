using KarafunAPI;
using Karayote.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karayote.Areas.Admin.Controllers
{
    [Authorize(Roles="Admin")]
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IKarafun _karafun;

        public AdminController(ILogger<HomeController> logger, IKarafun karafun)
        {
            _logger = logger;
            _karafun = karafun;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult StatusUpdate()
        {
            return new JsonResult(_karafun.Status);
        }
    }
}
