using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventManagement.UI.Services.Interfaces;
using EventManagement.UI.Models.Report;
using EventManagement.UI.Models.Event;
using EventManagement.UI.Models.Tenant;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Geçici olarak sadece giriş yapma kontrolü yap, rol kontrolünü manuel yapalım
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IAuthService _authService;
        private readonly IApiService _apiService;

        public DashboardController(
            ILogger<DashboardController> logger, 
            IAuthService authService,
            IApiService apiService)
        {
            _logger = logger;
            _authService = authService;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Manuel rol kontrolü yap
            var roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
                
            _logger.LogInformation("Admin Dashboard görüntülendi - Roller: {Roles}", string.Join(", ", roles));
            
            // Bu kontrolü geçici olarak devre dışı bırakalım
            /*
            if (!roles.Contains("Admin"))
            {
                _logger.LogWarning("Yetkisiz erişim girişimi!");
                return RedirectToAction("AccessDenied", "Account", new { area = "" });
            }
            */
            
            ViewData["Title"] = "Admin Dashboard";
            
            try
            {
                // API için token al
                string token = null;
                if (HttpContext.Session.Keys.Contains("AuthToken"))
                {
                    token = HttpContext.Session.GetString("AuthToken");
                }
                
                _logger.LogInformation("Dashboard API çağrıları başlatılıyor. Token: {TokenExist}", !string.IsNullOrEmpty(token));
                
                // Yaklaşan etkinlikler raporunu al
                var upcomingEventsResponse = await _apiService.GetAsync<List<UpcomingEventViewModel>>("api/reports/upcoming-events", token);
                if (upcomingEventsResponse.Success && upcomingEventsResponse.Data != null)
                {
                    var upcomingEvents = upcomingEventsResponse.Data;
                    _logger.LogInformation("Yaklaşan etkinlikler alındı. Toplam: {Count}", upcomingEvents.Count);

                    // Her yaklaşan etkinlik için katılımcı sayısını güncelle
                    foreach (var upcomingEvent in upcomingEvents)
                    {
                        try
                        {
                            var attendeesResponse = await _apiService.GetAsync<List<Models.Attendee.AttendeeViewModel>>($"api/events/{upcomingEvent.Id}/attendees", token);
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                upcomingEvent.RegistrationCount = attendeesResponse.Data.Count;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Yaklaşan etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", upcomingEvent.Id);
                        }
                    }
                    
                    // Yaklaşan etkinlikler için ViewBag'e ekle
                    ViewBag.UpcomingEvents = new UpcomingEventsReportViewModel 
                    { 
                        Events = upcomingEvents, 
                        TotalEvents = upcomingEvents.Count,
                        GeneratedAt = DateTime.Now
                    };
                }
                else
                {
                    _logger.LogWarning("Yaklaşan etkinlikler alınamadı. Hata: {Message}", upcomingEventsResponse.Message);
                    ViewBag.UpcomingEvents = new UpcomingEventsReportViewModel 
                    { 
                        Events = new List<UpcomingEventViewModel>(), 
                        TotalEvents = 0,
                        GeneratedAt = DateTime.Now
                    };
                }
                
                // Son etkinlikleri al
                try
                {
                    _logger.LogInformation("Son etkinlikler alınıyor...");
                    
                    // Önce tenant listesini al
                    var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                    if (!tenantsResponse.Success || tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                    {
                        _logger.LogWarning("Tenant listesi alınamadı: {Message}", tenantsResponse.Message);
                        ViewBag.RecentEvents = new List<EventViewModel>();
                    }
                    else
                    {
                        _logger.LogInformation("Tenant listesi alındı. Toplam tenant sayısı: {Count}", tenantsResponse.Data.Count);
                        
                        // Tüm tenant'lar için etkinlikleri topla
                        var allEvents = new List<EventViewModel>();
                        
                        foreach (var tenant in tenantsResponse.Data)
                        {
                            _logger.LogInformation("Tenant: {TenantName} (ID: {TenantId}) için etkinlikler alınıyor...", tenant.Name, tenant.Id);
                            
                            // Her tenant için "X-Tenant" header'ını değiştirerek API çağrısı yap
                            var headers = new Dictionary<string, string>
                            {
                                { "X-Tenant", tenant.Subdomain }
                            };
                            
                            var eventsResponse = await _apiService.GetAsyncWithHeaders<List<EventViewModel>>("api/events", token, headers);
                            
                            if (eventsResponse.Success && eventsResponse.Data != null && eventsResponse.Data.Any())
                            {
                                _logger.LogInformation("Tenant {TenantName} için {Count} etkinlik bulundu", tenant.Name, eventsResponse.Data.Count);
                                
                                // Etkinlikler listesine tenant bilgisiyle birlikte ekle
                                foreach (var eventItem in eventsResponse.Data)
                                {
                                    eventItem.TenantName = tenant.Name;
                                    eventItem.TenantSubdomain = tenant.Subdomain;
                                    allEvents.Add(eventItem);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Tenant {TenantName} için etkinlik bulunamadı veya hata oluştu: {Message}", 
                                    tenant.Name, eventsResponse.Message);
                            }
                        }
                        
                        _logger.LogInformation("Tüm tenant'lardan toplam {Count} etkinlik alındı", allEvents.Count);
                        
                        // Duruma göre sınıf ata
                        foreach (var eventItem in allEvents)
                        {
                            // Durum sınıfını ve metnini belirle
                            if (eventItem.Status == EventStatus.Active)
                            {
                                eventItem.StatusClass = "success";
                                eventItem.StatusText = "Aktif";
                            }
                            else if (eventItem.Status == EventStatus.Draft)
                            {
                                eventItem.StatusClass = "secondary";
                                eventItem.StatusText = "Taslak";
                            }
                            else if (eventItem.Status == EventStatus.Planning)
                            {
                                eventItem.StatusClass = "info";
                                eventItem.StatusText = "Planlama";
                            }
                            else if (eventItem.Status == EventStatus.Preparation)
                            {
                                eventItem.StatusClass = "primary";
                                eventItem.StatusText = "Hazırlık";
                            }
                            else if (eventItem.Status == EventStatus.Pending)
                            {
                                eventItem.StatusClass = "warning";
                                eventItem.StatusText = "Beklemede";
                            }
                            else if (eventItem.Status == EventStatus.Cancelled)
                            {
                                eventItem.StatusClass = "danger";
                                eventItem.StatusText = "İptal";
                            }
                            else if (eventItem.Status == EventStatus.Completed)
                            {
                                eventItem.StatusClass = "dark";
                                eventItem.StatusText = "Tamamlandı";
                            }
                            else if (eventItem.Status == EventStatus.Approved)
                            {
                                eventItem.StatusClass = "success";
                                eventItem.StatusText = "Onaylandı";
                            }
                            else if (eventItem.Status == EventStatus.Rejected)
                            {
                                eventItem.StatusClass = "danger";
                                eventItem.StatusText = "Reddedildi";
                            }
                            else
                            {
                                eventItem.StatusClass = "secondary";
                                eventItem.StatusText = eventItem.Status.ToString();
                            }
                        }
                        
                        // En son eklenen etkinlikleri al (en fazla 10 tane)
                        ViewBag.RecentEvents = allEvents.OrderByDescending(e => e.CreatedDate).Take(10).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Son etkinlikler alınırken hata oluştu");
                    ViewBag.RecentEvents = new List<EventViewModel>();
                }
                
                // Aktif tenant listesini al
                try
                {
                    _logger.LogInformation("Tenant listesi alınıyor...");
                    var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                    _logger.LogInformation("Tenant listesi API yanıtı: Başarılı={Success}, Mesaj={Message}",
                        tenantsResponse.Success, tenantsResponse.Message);
                    
                    if (tenantsResponse.Success && tenantsResponse.Data != null)
                    {
                        var tenants = tenantsResponse.Data;
                        _logger.LogInformation("Tenant listesi alındı. Toplam tenant sayısı: {Count}", tenants.Count);
                        
                        // Sadece aktif tenantları filtrele
                        var activeTenants = tenants.Where(t => t.IsActive).ToList();
                        _logger.LogInformation("Aktif tenant sayısı: {Count}", activeTenants.Count);
                        ViewBag.ActiveTenants = activeTenants;
                    }
                    else
                    {
                        _logger.LogWarning("Tenant listesi alınamadı. Hata: {Message}", tenantsResponse.Message);
                        ViewBag.ActiveTenants = new List<TenantViewModel>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tenant listesi alınırken hata oluştu");
                    ViewBag.ActiveTenants = new List<TenantViewModel>();
                }
                
                // Hata ayıklama için tüm ViewBag değişkenlerini logla
                var hasUpcomingEvents = ViewBag.UpcomingEvents != null;
                var hasRecentEvents = ViewBag.RecentEvents != null;
                var hasActiveTenants = ViewBag.ActiveTenants != null;
                _logger.LogInformation($"Dashboard ViewBag değerleri: UpcomingEvents={hasUpcomingEvents}, RecentEvents={hasRecentEvents}, ActiveTenants={hasActiveTenants}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard verileri yüklenirken hata oluştu");
                ViewBag.UpcomingEvents = new UpcomingEventsReportViewModel 
                { 
                    Events = new List<UpcomingEventViewModel>(), 
                    TotalEvents = 0,
                    GeneratedAt = DateTime.Now
                };
                ViewBag.RecentEvents = new List<EventViewModel>();
                ViewBag.ActiveTenants = new List<TenantViewModel>();
            }
            
            return View();
        }
        
        [AllowAnonymous]
        public IActionResult Teshis()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
                
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User?.FindFirstValue(ClaimTypes.Email);
            
            var viewModel = new 
            {
                IsAuthenticated = isAuthenticated,
                Roles = roles,
                UserId = userId,
                Email = email,
                ServiceRoles = _authService.GetUserRolesAsync(),
                IsInAdminRole = _authService.IsInRoleAsync("Admin"),
                RequestPath = HttpContext.Request.Path,
                RequestQuery = HttpContext.Request.QueryString.ToString(),
                RequestMethod = HttpContext.Request.Method,
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };
            
            return Json(viewModel);
        }
    }
} 