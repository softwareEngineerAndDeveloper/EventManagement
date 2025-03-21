using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.UI.Areas.Attendee.Controllers
{
    [Area("Attendee")]
    [Authorize(Roles = "Attendee")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Attendee Dashboard görüntülendi");
            ViewData["Title"] = "Katılımcı Dashboard";
            return View();
        }
    }
} 