using EventManagement.UI.Interfaces;
using EventManagement.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using EventManagement.UI.DTOs; // Session için

namespace EventManagement.UI.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IApiServiceUI _apiService;

        public HomeController(ILogger<HomeController> logger, IApiServiceUI apiService) 
            : base(logger)
        {
            _apiService = apiService;
        }

        private Guid GetTenantId()
        {
            var tenantIdStr = HttpContext.Session.GetString("TenantId");
            
            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out Guid tenantId))
            {
                return tenantId;
            }
            
            // Varsayılan tenant ID
            return new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        }

        public async Task<IActionResult> Index()
        {
            var upcomingEvents = await _apiService.GetUpcomingEventsAsync(GetTenantId());
            
            if (!upcomingEvents.IsSuccess)
            {
                _logger.LogError("Yaklaşan etkinlikler alınamadı: {Message}", upcomingEvents.Message);
                upcomingEvents.Data = new List<EventDto>();
            }
            
            return View(upcomingEvents.Data);
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