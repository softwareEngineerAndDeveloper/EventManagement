using EventManagement.UI.Models.Attendee;
using EventManagement.UI.Models.Event;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.Tenant;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AttendeeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<AttendeeController> _logger;

        public AttendeeController(IApiService apiService, IAuthService authService, ILogger<AttendeeController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success || tenantsResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "Tenant listesi alınamadı";
                    return View(new List<AttendeeViewModel>());
                }
                
                var allAttendees = new List<AttendeeViewModel>();
                
                // Her tenant için işlemleri yap
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    // Önce tüm etkinlikleri al
                    var eventsResponse = await _apiService.GetAsyncWithHeaders<List<EventViewModel>>("api/events", token, headers);
                    
                    if (eventsResponse.Success && eventsResponse.Data != null)
                    {
                        // Her etkinlik için katılımcıları al
                        foreach (var eventItem in eventsResponse.Data)
                        {
                            var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>(
                                $"api/events/{eventItem.Id}/attendees", token, headers);
                                
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                foreach (var attendee in attendeesResponse.Data)
                                {
                                    attendee.EventName = eventItem.Title;
                                    allAttendees.Add(attendee);
                                }
                            }
                        }
                    }
                }
                
                return View(allAttendees);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return View(new List<AttendeeViewModel>());
            }
        }
        
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["ErrorMessage"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                
                _logger.LogInformation("Katılımcı detayları görüntüleniyor... ID: {AttendeeId}", id);
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success || tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi alınamadı");
                    TempData["ErrorMessage"] = "Tenant listesi alınamadı.";
                    return RedirectToAction(nameof(Index));
                }
                
                AttendeeViewModel? attendee = null;
                string tenantName = string.Empty;
                string tenantSubdomain = string.Empty;
                
                // Her tenant için katılımcıyı aramaya çalışalım
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için katılımcı {AttendeeId} aranıyor...", tenant.Name, id);
                    
                    var response = await _apiService.GetAsyncWithHeaders<AttendeeViewModel>($"api/attendees/{id}", token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        _logger.LogInformation("Katılımcı {AttendeeId} bulundu, Tenant: {TenantName}", id, tenant.Name);
                        attendee = response.Data;
                        tenantName = tenant.Name;
                        tenantSubdomain = tenant.Subdomain;
                        
                        // Tenant bilgilerini ViewBag'e ekle
                        ViewBag.TenantName = tenantName;
                        ViewBag.TenantSubdomain = tenantSubdomain;
                        
                        // Etkinlik bilgilerini al
                        try
                        {
                            var eventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>(
                                $"api/events/{attendee.EventId}", token, headers);
                                
                            if (eventResponse.Success && eventResponse.Data != null)
                            {
                                attendee.EventName = eventResponse.Data.Title;
                            }
                            else
                            {
                                attendee.EventName = "Etkinlik Bulunamadı";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Etkinlik bilgileri alınırken hata oluştu. EventId: {EventId}", attendee.EventId);
                            attendee.EventName = "Hata";
                        }
                        
                        break; // Katılımcıyı bulduk, döngüyü sonlandır
                    }
                }
                
                if (attendee == null)
                {
                    _logger.LogWarning("Katılımcı bulunamadı. ID: {AttendeeId}", id);
                    TempData["ErrorMessage"] = "Katılımcı bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(attendee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı detayları görüntülenirken hata oluştu. ID: {AttendeeId}", id);
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 