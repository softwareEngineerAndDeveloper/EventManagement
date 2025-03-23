using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using EventManagement.UI.Models.Dashboard;

namespace EventManagement.UI.Areas.EventManager.Controllers
{
    [Area("EventManager")]
    [Authorize(Roles = "EventManager")]
    public class HomeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IApiService apiService, ILogger<HomeController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = HttpContext.Session.GetString("Token");
                
                // Dashboard istatistiklerini al
                var dashboardStatsResponse = await _apiService.GetAsync<DashboardStatsViewModel>("api/reports/dashboard-stats", token);
                if (dashboardStatsResponse.Success)
                {
                    ViewBag.DashboardStats = dashboardStatsResponse.Data;
                }
                else
                {
                    _logger.LogWarning("Dashboard istatistikleri alınamadı: {Message}", dashboardStatsResponse.Message);
                    ViewBag.DashboardStats = new DashboardStatsViewModel
                    {
                        TotalEvents = 0,
                        ActiveEvents = 0,
                        UpcomingEvents = 0,
                        TotalRegistrations = 0,
                        TotalUsers = 0
                    };
                }
                
                // Yaklaşan etkinlikleri çek
                var upcomingEventsResponse = await _apiService.GetAsync<List<dynamic>>("api/events/upcoming", token);
                ViewBag.UpcomingEvents = upcomingEventsResponse.Success ? upcomingEventsResponse.Data : new List<dynamic>();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventManager Panel sayfası yüklenirken hata oluştu");
                ViewBag.DashboardStats = new DashboardStatsViewModel
                {
                    TotalEvents = 0,
                    ActiveEvents = 0,
                    UpcomingEvents = 0,
                    TotalRegistrations = 0,
                    TotalUsers = 0
                };
                ViewBag.UpcomingEvents = new List<dynamic>();
                return View();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 