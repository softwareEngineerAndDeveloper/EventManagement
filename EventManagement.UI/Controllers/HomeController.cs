using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EventManagement.UI.Models;
using EventManagement.UI.Services;

namespace EventManagement.UI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IApiService _apiService;

    public HomeController(ILogger<HomeController> logger, IApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        var upcomingEvents = await _apiService.GetUpcomingEventsAsync();
        return View(upcomingEvents.Data ?? new List<Models.DTOs.EventDto>());
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
