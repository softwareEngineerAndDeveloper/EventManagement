using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EventManagement.UI.Models;

namespace EventManagement.UI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var errorModel = new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        };

        if (statusCode.HasValue)
        {
            errorModel.StatusCode = statusCode.Value;
            
            switch (statusCode.Value)
            {
                case 401:
                    errorModel.Title = "Yetkisiz Erişim";
                    errorModel.Message = "Bu sayfaya erişmek için giriş yapmanız gerekmektedir.";
                    break;
                case 403:
                    errorModel.Title = "Erişim Reddedildi";
                    errorModel.Message = "Bu sayfaya erişim izniniz bulunmamaktadır.";
                    break;
                case 404:
                    errorModel.Title = "Sayfa Bulunamadı";
                    errorModel.Message = "Aradığınız sayfa bulunamadı.";
                    break;
                case 500:
                default:
                    errorModel.Title = "Bir Hata Oluştu";
                    errorModel.Message = "İşleminiz gerçekleştirilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
                    break;
            }
        }
        else
        {
            errorModel.Title = "Bir Hata Oluştu";
            errorModel.Message = "İşleminiz gerçekleştirilirken beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
        }

        // Loglama
        _logger.LogError("Hata: {Title} - {Message} - RequestId: {RequestId}", 
            errorModel.Title, errorModel.Message, errorModel.RequestId);

        return View(errorModel);
    }
}
