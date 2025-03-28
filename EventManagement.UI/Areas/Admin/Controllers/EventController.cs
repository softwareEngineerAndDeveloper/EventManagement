using EventManagement.UI.Models.Attendee;
using EventManagement.UI.Models.Event;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.Tenant;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<EventController> _logger;
        private readonly IConfiguration _configuration;

        public EventController(IApiService apiService, IAuthService authService, ILogger<EventController> logger, IConfiguration configuration)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        private async Task<bool> TryGetTenantEvents(string token, TenantViewModel tenant, List<EventViewModel> eventsCollection)
        {
            try
            {
                _logger.LogInformation("Tenant: {TenantName} (ID: {TenantId}) için etkinlikler alınıyor...", tenant.Name, tenant.Id);
                
                // Her tenant için "X-Tenant" header'ını belirle
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
                        eventsCollection.Add(eventItem);
                    }
                    return true;
                }
                else
                {
                    _logger.LogWarning("Tenant {TenantName} için etkinlik bulunamadı veya hata oluştu: {Message}", 
                        tenant.Name, eventsResponse.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant {TenantName} için etkinlikler alınırken hata oluştu", tenant.Name);
                return false;
            }
        }
        
        public async Task<IActionResult> Index()
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
                
                _logger.LogInformation("Etkinlikler API'den alınıyor... Token mevcut: {HasToken}", true);
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    return View(new List<EventViewModel>());
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                    return View(new List<EventViewModel>());
                }
                
                _logger.LogInformation("Tenant listesi alındı. Toplam tenant sayısı: {Count}", tenantsResponse.Data.Count);
                
                // Tüm tenant'lar için etkinlikleri topla
                var allEvents = new List<EventViewModel>();
                int successCount = 0;
                int failCount = 0;
                
                foreach (var tenant in tenantsResponse.Data)
                {
                    bool success = await TryGetTenantEvents(token, tenant, allEvents);
                    if (success) 
                        successCount++;
                    else
                        failCount++;
                }
                
                _logger.LogInformation("Etkinlik alma işlemi tamamlandı. Başarılı tenant sayısı: {SuccessCount}, Başarısız: {FailCount}", 
                    successCount, failCount);
                
                if (allEvents.Any())
                {
                    _logger.LogInformation("Tüm tenant'lardan toplam {Count} etkinlik alındı", allEvents.Count);
                    
                    // Etkinlik durumlarını ön tarafta göstermek için gerekli sınıfları ekle
                    foreach (var eventItem in allEvents)
                    {
                        // Durum sınıfını ve metnini belirle
                        switch (eventItem.Status)
                        {
                            case EventStatus.Active:
                                eventItem.StatusClass = "success";
                                eventItem.StatusText = "Aktif";
                                break;
                            case EventStatus.Draft:
                                eventItem.StatusClass = "secondary";
                                eventItem.StatusText = "Taslak";
                                break;
                            case EventStatus.Planning:
                                eventItem.StatusClass = "info";
                                eventItem.StatusText = "Planlama";
                                break;
                            case EventStatus.Preparation:
                                eventItem.StatusClass = "primary";
                                eventItem.StatusText = "Hazırlık";
                                break;
                            case EventStatus.Pending:
                                eventItem.StatusClass = "warning";
                                eventItem.StatusText = "Beklemede";
                                break;
                            case EventStatus.Cancelled:
                                eventItem.StatusClass = "danger";
                                eventItem.StatusText = "İptal";
                                break;
                            case EventStatus.Completed:
                                eventItem.StatusClass = "dark";
                                eventItem.StatusText = "Tamamlandı";
                                break;
                            case EventStatus.Approved:
                                eventItem.StatusClass = "success";
                                eventItem.StatusText = "Onaylandı";
                                break;
                            case EventStatus.Rejected:
                                eventItem.StatusClass = "danger";
                                eventItem.StatusText = "Reddedildi";
                                break;
                            default:
                                eventItem.StatusClass = "secondary";
                                eventItem.StatusText = eventItem.Status.ToString();
                                break;
                        }
                    }
                    
                    // Her etkinlik için katılımcı sayısını alırken hata yönetimini geliştir
                    foreach (var eventItem in allEvents)
                    {
                        try
                        {
                            var headers = new Dictionary<string, string>
                            {
                                { "X-Tenant", eventItem.TenantSubdomain }
                            };
                            
                            var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{eventItem.Id}/attendees", token, headers);
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                eventItem.RegistrationCount = attendeesResponse.Data.Count;
                            }
                            else
                            {
                                _logger.LogWarning("Etkinlik {EventId} için katılımcı sayısı alınamadı: {Message}", 
                                    eventItem.Id, attendeesResponse.Message);
                                eventItem.RegistrationCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", eventItem.Id);
                            eventItem.RegistrationCount = 0;
                        }
                    }
                    
                    return View(allEvents);
                }
                else
                {
                    _logger.LogInformation("Hiç etkinlik bulunamadı");
                    return View(new List<EventViewModel>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlikler listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Etkinlikler listelenirken beklenmeyen bir hata oluştu: " + ex.Message;
                return View(new List<EventViewModel>());
            }
        }

        // Etkinlik Detayları
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
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    return RedirectToAction(nameof(Index));
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                    return RedirectToAction(nameof(Index));
                }
                
                EventViewModel? eventData = null;
                
                // Her tenant için etkinliği aramaya çalışalım
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için etkinlik {EventId} aranıyor...", tenant.Name, id);
                    
                    var response = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{id}", token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        _logger.LogInformation("Etkinlik {EventId} bulundu, Tenant: {TenantName}", id, tenant.Name);
                        eventData = response.Data;
                        
                        // Tenant bilgilerini ekleyelim
                        eventData.TenantName = tenant.Name;
                        eventData.TenantSubdomain = tenant.Subdomain;
                        
                        // Katılımcı sayısını al ve güncelle
                        try
                        {
                            var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{id}/attendees", token, headers);
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                eventData.RegistrationCount = attendeesResponse.Data.Count;
                                _logger.LogInformation("Etkinlik {EventId} için katılımcı sayısı güncellendi: {Count}", id, eventData.RegistrationCount);
                            }
                            else
                            {
                                _logger.LogWarning("Etkinlik {EventId} için katılımcı sayısı alınamadı: {Message}", 
                                    id, attendeesResponse.Message);
                                eventData.RegistrationCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", id);
                            eventData.RegistrationCount = 0;
                        }
                        
                        break; // Etkinliği bulduk, döngüyü sonlandır
                    }
                }
                
                if (eventData != null)
                {
                    return View(eventData);
                }
                
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik detayları getirilirken hata oluştu. ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik detayları getirilirken beklenmeyen bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Etkinlik Düzenleme Formu
        public async Task<IActionResult> Edit(Guid id)
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
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    return RedirectToAction(nameof(Index));
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                    return RedirectToAction(nameof(Index));
                }
                
                EventViewModel? eventData = null;
                string? tenantSubdomain = null;
                
                // Her tenant için etkinliği aramaya çalışalım
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için etkinlik {EventId} aranıyor...", tenant.Name, id);
                    
                    var response = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{id}", token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        _logger.LogInformation("Etkinlik {EventId} bulundu, Tenant: {TenantName}", id, tenant.Name);
                        eventData = response.Data;
                        tenantSubdomain = tenant.Subdomain;
                        
                        // Tenant bilgilerini ekleyelim
                        eventData.TenantName = tenant.Name;
                        eventData.TenantSubdomain = tenant.Subdomain;
                        
                        // Katılımcı sayısını al ve güncelle
                        try
                        {
                            var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{id}/attendees", token, headers);
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                eventData.RegistrationCount = attendeesResponse.Data.Count;
                                _logger.LogInformation("Etkinlik {EventId} için katılımcı sayısı güncellendi: {Count}", id, eventData.RegistrationCount);
                            }
                            else
                            {
                                _logger.LogWarning("Etkinlik {EventId} için katılımcı sayısı alınamadı: {Message}", 
                                    id, attendeesResponse.Message);
                                eventData.RegistrationCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", id);
                            eventData.RegistrationCount = 0;
                        }
                        
                        break; // Etkinliği bulduk, döngüyü sonlandır
                    }
                }
                
                if (eventData != null && !string.IsNullOrEmpty(tenantSubdomain))
                {
                    // Düzenleme işlemi için tenant bilgisini TempData'ya kaydet
                    // Session yerine TempData kullanarak daha kısa süreli ve güvenli saklama
                    TempData["EditEventTenant"] = tenantSubdomain;
                    
                    return View(eventData);
                }
                
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik düzenleme formu açılırken hata oluştu. ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik düzenleme formu açılırken beklenmeyen bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Etkinlik Güncelleme İşlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EventViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Geçersiz etkinlik ID'si.";
                return RedirectToAction(nameof(Index));
            }
            
            if (ModelState.IsValid)
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
                    
                    // TempData'dan tenant bilgisini al
                    string tenantSubdomain = "default";
                    if (TempData.ContainsKey("EditEventTenant"))
                    {
                        tenantSubdomain = TempData["EditEventTenant"].ToString();
                        // TempData değerini tutmak istiyorsak tekrar eklemeliyiz
                        TempData.Keep("EditEventTenant");
                    }
                    
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenantSubdomain }
                    };
                    
                    _logger.LogInformation("Etkinlik güncelleniyor. ID: {EventId}, Tenant: {TenantSubdomain}", id, tenantSubdomain);
                    var response = await _apiService.PutAsyncWithHeaders<EventViewModel>($"api/events/{id}", model, token, headers);
                    
                    if (response.Success)
                    {
                        TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    TempData["ErrorMessage"] = $"Etkinlik güncellenemedi: {response.Message}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Etkinlik güncellenirken hata oluştu. ID: {Id}", id);
                    TempData["ErrorMessage"] = "Etkinlik güncellenirken beklenmeyen bir hata oluştu: " + ex.Message;
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Etkinlik güncelleme formunda doğrulama hataları: {Errors}", string.Join(", ", errors));
                TempData["ErrorMessage"] = "Lütfen gerekli alanları doğru şekilde doldurun.";
            }
            
            return View(model);
        }
        
        // Etkinlik Silme Onay Sayfası
        public async Task<IActionResult> Delete(Guid id)
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
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    return RedirectToAction(nameof(Index));
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                    return RedirectToAction(nameof(Index));
                }
                
                EventViewModel? eventData = null;
                string? tenantSubdomain = null;
                
                // Her tenant için etkinliği aramaya çalışalım
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için etkinlik {EventId} aranıyor...", tenant.Name, id);
                    
                    var response = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{id}", token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        _logger.LogInformation("Etkinlik {EventId} bulundu, Tenant: {TenantName}", id, tenant.Name);
                        eventData = response.Data;
                        tenantSubdomain = tenant.Subdomain;
                        
                        // Tenant bilgilerini ekleyelim
                        eventData.TenantName = tenant.Name;
                        eventData.TenantSubdomain = tenant.Subdomain;
                        
                        // Katılımcı sayısını al ve güncelle
                        try
                        {
                            var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{id}/attendees", token, headers);
                            if (attendeesResponse.Success && attendeesResponse.Data != null)
                            {
                                eventData.RegistrationCount = attendeesResponse.Data.Count;
                                _logger.LogInformation("Etkinlik {EventId} için katılımcı sayısı güncellendi: {Count}", id, eventData.RegistrationCount);
                            }
                            else
                            {
                                _logger.LogWarning("Etkinlik {EventId} için katılımcı sayısı alınamadı: {Message}", 
                                    id, attendeesResponse.Message);
                                eventData.RegistrationCount = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", id);
                            eventData.RegistrationCount = 0;
                        }
                        
                        break; // Etkinliği bulduk, döngüyü sonlandır
                    }
                }
                
                if (eventData != null && !string.IsNullOrEmpty(tenantSubdomain))
                {
                    // Silme işlemi için tenant bilgisini TempData'ya kaydet
                    TempData["DeleteEventTenant"] = tenantSubdomain;
                    
                    return View(eventData);
                }
                
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik silme onay sayfası açılırken hata oluştu. ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik silme onay sayfası açılırken beklenmeyen bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Etkinlik Silme İşlemi
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
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
                
                // TempData'dan tenant bilgisini al
                string tenantSubdomain = "default";
                if (TempData.ContainsKey("DeleteEventTenant"))
                {
                    tenantSubdomain = TempData["DeleteEventTenant"].ToString();
                    // Bu değeri bir sonraki istekte kullanmayacağız
                }
                
                var headers = new Dictionary<string, string>
                {
                    { "X-Tenant", tenantSubdomain }
                };
                
                _logger.LogInformation("Etkinlik siliniyor. ID: {EventId}, Tenant: {TenantSubdomain}", id, tenantSubdomain);
                
                var response = await _apiService.DeleteAsyncWithHeaders<bool>($"api/events/{id}", token, headers);
                
                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla silindi.";
                    return RedirectToAction(nameof(Index));
                }
                
                TempData["ErrorMessage"] = $"Etkinlik silinemedi: {response.Message}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik silinirken hata oluştu. ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik silinirken beklenmeyen bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Etkinlik Katılımcıları
        public async Task<IActionResult> Attendees(Guid id)
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
                
                _logger.LogInformation("Etkinlik katılımcıları alınıyor... Etkinlik ID: {EventId}", id);
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    return RedirectToAction(nameof(Index));
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                    return RedirectToAction(nameof(Index));
                }
                
                EventViewModel? eventData = null;
                Dictionary<string, string>? foundHeaders = null;
                List<AttendeeViewModel>? attendeesList = null;
                
                // Her tenant için etkinliği aramaya çalışalım
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için etkinlik {EventId} aranıyor...", tenant.Name, id);
                    
                    var response = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{id}", token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        _logger.LogInformation("Etkinlik {EventId} bulundu, Tenant: {TenantName}", id, tenant.Name);
                        eventData = response.Data;
                        foundHeaders = headers;
                        
                        // Tenant bilgilerini ekleyelim
                        eventData.TenantName = tenant.Name;
                        eventData.TenantSubdomain = tenant.Subdomain;
                        
                        // Katılımcıları al
                        var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{id}/attendees", token, headers);
                        
                        if (attendeesResponse.Success && attendeesResponse.Data != null)
                        {
                            attendeesList = attendeesResponse.Data;
                            eventData.RegistrationCount = attendeesList.Count;
                            _logger.LogInformation("Etkinlik {EventId} için {Count} katılımcı bulundu", id, attendeesList.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Etkinlik {EventId} için katılımcı listesi alınamadı: {StatusCode} - {Message}", 
                                id, attendeesResponse.StatusCode, attendeesResponse.Message);
                            attendeesList = new List<AttendeeViewModel>();
                        }
                        
                        break; // Etkinliği bulduk, döngüyü sonlandır
                    }
                }
                
                if (eventData == null)
                {
                    _logger.LogWarning("Etkinlik bulunamadı. ID: {EventId}", id);
                    TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }
                
                // İşlemler için tenant bilgisini TempData'ya kaydet (Session yerine)
                if (foundHeaders != null)
                {
                    TempData["EventTenant"] = eventData.TenantSubdomain;
                    // Bir sonraki istek için değeri koruyalım
                    TempData.Keep("EventTenant");
                }
                
                ViewBag.Event = eventData;
                ViewBag.EventId = id;
                
                return View(attendeesList ?? new List<AttendeeViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik katılımcıları getirilirken hata oluştu. EventID: {Id}", id);
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // Etkinliğe Katılımcı Ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendee(CreateAttendeeViewModel model)
        {
            try
            {
                // TenantId değerini logla
                _logger.LogInformation("Gönderilen TenantId: {TenantId}", model.TenantId);
                
                if (ModelState.IsValid)
                {
                    // AuthService üzerinden token al
                    string token = await _authService.GetTokenAsync();
                    
                    if (string.IsNullOrEmpty(token))
                    {
                        _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                        TempData["ErrorMessage"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    
                    // TempData'dan tenant bilgisini al
                    string tenantSubdomain = "default";
                    if (TempData.ContainsKey("EventTenant"))
                    {
                        tenantSubdomain = TempData["EventTenant"].ToString();
                        // Bir sonraki istek için değeri koruyalım
                        TempData.Keep("EventTenant");
                    }
                    
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenantSubdomain }
                    };
                    
                    _logger.LogInformation("Katılımcı eklenecek etkinlik - EventId: {EventId}, TenantSubdomain: {TenantSubdomain}", 
                        model.EventId, tenantSubdomain);
                    
                    // Önce mevcut katılımcıları kontrol et - aynı e-posta zaten var mı?
                    var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>($"api/events/{model.EventId}/attendees", token, headers);
                    if (attendeesResponse.Success && attendeesResponse.Data != null)
                    {
                        bool emailExists = attendeesResponse.Data.Any(a => a.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                        if (emailExists)
                        {
                            _logger.LogWarning("E-posta adresi zaten kayıtlı: {Email}", model.Email);
                            TempData["EmailExists"] = true;
                            TempData["ExistingEmail"] = model.Email;
                            TempData["ErrorMessage"] = "Bu e-posta adresi zaten etkinliğe kayıtlı.";
                            return RedirectToAction(nameof(Attendees), new { id = model.EventId });
                        }
                    }
                    
                    // API'ye gönderilecek tüm veriyi logla
                    _logger.LogInformation("API'ye gönderilen veri: EventId={EventId}, TenantId={TenantId}, Name={Name}, Email={Email}", 
                        model.EventId, model.TenantId, model.Name, model.Email);
                    
                    var response = await _apiService.PostAsyncWithHeaders<AttendeeViewModel>($"api/events/{model.EventId}/attendees", model, token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        // Katılımcı eklendikten sonra etkinliği güncelle (RegistrationCount arttırmak için)
                        var updatedEventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{model.EventId}", token, headers);
                        if (updatedEventResponse.Success && updatedEventResponse.Data != null)
                        {
                            var eventModel = updatedEventResponse.Data;
                            eventModel.RegistrationCount++; // Kayıt sayısını bir artır
                            await _apiService.PutAsyncWithHeaders<EventViewModel>($"api/events/{model.EventId}", eventModel, token, headers);
                        }
                        
                        TempData["SuccessMessage"] = "Katılımcı başarıyla eklendi.";
                        
                        // E-posta gönderim simülasyonu
                        if (model.SendEmailNotification)
                        {
                            _logger.LogInformation("E-posta gönderimi simüle ediliyor: {Email}", model.Email);
                            
                            // Etkinlik bilgisini al
                            var eventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{model.EventId}", token, headers);
                            string eventTitle = eventResponse.Success && eventResponse.Data != null 
                                ? eventResponse.Data.Title 
                                : "Etkinlik";
                                
                            // E-posta gönderim bilgilerini TempData'ya ekle
                            TempData["EmailSent"] = true;
                            TempData["EmailAddress"] = model.Email;
                            TempData["EmailSubject"] = $"{eventTitle} etkinliğine kayıt onayı";
                        }
                        
                        return RedirectToAction(nameof(Attendees), new { id = model.EventId });
                    }
                    
                    TempData["ErrorMessage"] = $"Katılımcı eklenemedi: {response.Message}";
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["ErrorMessage"] = $"Doğrulama hataları: {string.Join(", ", errors)}";
                }
                
                return RedirectToAction(nameof(Attendees), new { id = model.EventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı eklenirken hata oluştu. EventID: {EventId}", model.EventId);
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Attendees), new { id = model.EventId });
            }
        }
        
        // Katılımcı Sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendee(Guid id, Guid eventId)
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
                
                // TempData'dan tenant bilgisini al
                string tenantSubdomain = "default";
                if (TempData.ContainsKey("EventTenant"))
                {
                    tenantSubdomain = TempData["EventTenant"].ToString();
                    // Bir sonraki istek için değeri koruyalım
                    TempData.Keep("EventTenant");
                    _logger.LogInformation("TempData'dan tenant subdomain alındı: {Subdomain}", tenantSubdomain);
                }
                
                var headers = new Dictionary<string, string>
                {
                    { "X-Tenant", tenantSubdomain }
                };
                
                _logger.LogInformation("Katılımcı siliniyor. ID: {AttendeeId}, EventID: {EventId}, Tenant: {TenantSubdomain}", 
                    id, eventId, tenantSubdomain);
                
                var response = await _apiService.DeleteAsyncWithHeaders<bool>($"api/events/attendees/{id}", token, headers);
                
                if (response.Success && Convert.ToBoolean(response.Data))
                {
                    // Katılımcı silindikten sonra etkinliği güncelle (RegistrationCount azaltmak için)
                    var updatedEventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{eventId}", token, headers);
                    if (updatedEventResponse.Success && updatedEventResponse.Data != null)
                    {
                        var eventModel = updatedEventResponse.Data;
                        if (eventModel.RegistrationCount > 0) // Eksi değere düşmemesi için kontrol
                        {
                            eventModel.RegistrationCount--; // Kayıt sayısını bir azalt
                            await _apiService.PutAsyncWithHeaders<EventViewModel>($"api/events/{eventId}", eventModel, token, headers);
                        }
                    }
                    
                    TempData["SuccessMessage"] = "Katılımcı başarıyla silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Katılımcı silinemedi: {response.Message}";
                }
                
                return RedirectToAction(nameof(Attendees), new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı silinirken hata oluştu. ID: {Id}, EventID: {EventId}", id, eventId);
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Attendees), new { id = eventId });
            }
        }
        
        // Katılım Durumu Güncelle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendeeStatus(Guid id, Guid eventId, AttendeeStatus status, bool hasAttended, bool sendEmailNotification)
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
                
                // TempData'dan tenant bilgisini al
                string tenantSubdomain = "default";
                if (TempData.ContainsKey("EventTenant"))
                {
                    tenantSubdomain = TempData["EventTenant"].ToString();
                    // Bir sonraki istek için değeri koruyalım
                    TempData.Keep("EventTenant");
                    _logger.LogInformation("TempData'dan tenant subdomain alındı: {Subdomain}", tenantSubdomain);
                }
                
                var headers = new Dictionary<string, string>
                {
                    { "X-Tenant", tenantSubdomain }
                };
                
                _logger.LogInformation("Katılımcı durumu güncelleniyor. ID: {AttendeeId}, EventID: {EventId}, Tenant: {TenantSubdomain}, Status: {Status}", 
                    id, eventId, tenantSubdomain, status);
                
                // Önce mevcut katılımcı bilgilerini al
                var getResponse = await _apiService.GetAsyncWithHeaders<AttendeeViewModel>($"api/events/attendees/{id}", token, headers);
                
                if (!getResponse.Success || getResponse.Data == null)
                {
                    TempData["ErrorMessage"] = $"Katılımcı bilgileri alınamadı: {getResponse.Message}";
                    return RedirectToAction(nameof(Attendees), new { id = eventId });
                }
                
                var attendee = getResponse.Data;
                attendee.Status = status;
                attendee.HasAttended = hasAttended;
                attendee.SendEmailNotification = sendEmailNotification;
                
                var updateResponse = await _apiService.PutAsyncWithHeaders<AttendeeViewModel>($"api/events/attendees/{id}", attendee, token, headers);
                
                if (updateResponse.Success)
                {
                    TempData["SuccessMessage"] = "Katılımcı durumu başarıyla güncellendi.";
                    
                    // Durum değişikliği e-posta bildirimi simülasyonu
                    if (sendEmailNotification)
                    {
                        _logger.LogInformation("Durum değişikliği e-posta bildirimi simüle ediliyor: {Email}", attendee.Email);
                        
                        // Etkinlik bilgisini al
                        var eventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{eventId}", token, headers);
                        string eventTitle = eventResponse.Success && eventResponse.Data != null 
                            ? eventResponse.Data.Title 
                            : "Etkinlik";
                            
                            string statusText = status switch
                            {
                                AttendeeStatus.Confirmed => "onaylandı",
                                AttendeeStatus.Cancelled => "iptal edildi",
                                AttendeeStatus.Pending => "beklemede",
                                _ => "güncellendi"
                            };
                            
                            // E-posta gönderim bilgilerini TempData'ya ekle
                            TempData["EmailSent"] = true;
                            TempData["EmailAddress"] = attendee.Email;
                            TempData["EmailSubject"] = $"{eventTitle} etkinliğine katılım durumunuz {statusText}";
                        }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Katılımcı durumu güncellenemedi: {updateResponse.Message}";
                }
                
                return RedirectToAction(nameof(Attendees), new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı durumu güncellenirken hata oluştu. ID: {Id}, EventID: {EventId}", id, eventId);
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Attendees), new { id = eventId });
            }
        }

        // Yeni etkinlik oluşturma formunu görüntüler
        public async Task<IActionResult> Create()
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
                
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    TempData["ErrorMessage"] = $"Tenant listesi alınamadı: {errorDetail}";
                    ViewBag.Tenants = new List<TenantViewModel>();
                }
                else if (tenantsResponse.Data != null)
                {
                    ViewBag.Tenants = tenantsResponse.Data;
                    _logger.LogInformation("Tenant listesi başarıyla alındı: {Count} tenant", tenantsResponse.Data.Count);
                }
                else
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    ViewBag.Tenants = new List<TenantViewModel>();
                    TempData["ErrorMessage"] = "Tenant listesi boş. Sistem yöneticinize başvurun.";
                }
                
                // Yeni bir model oluşturup varsayılan değerleri ayarla
                var model = new EventCreateViewModel
                {
                    StartDate = DateTime.Now.AddDays(1),
                    EndDate = DateTime.Now.AddDays(1).AddHours(2),
                    MaxAttendees = 100,
                    Capacity = 100,
                    IsPublic = true
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create sayfası yüklenirken hata oluştu");
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                ViewBag.Tenants = new List<TenantViewModel>();
                return View(new EventCreateViewModel());
            }
        }
        
        // Yeni etkinlik oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model, string SelectedTenant)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // AuthService üzerinden token al
                    string token = await _authService.GetTokenAsync();
                    
                    if (string.IsNullOrEmpty(token))
                    {
                        _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                        TempData["ErrorMessage"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                        return RedirectToAction("Login", "Account", new { area = "" });
                    }
                    
                    // Kullanıcı tarafından seçilen tenant bilgisini al
                    string tenantSubdomain = !string.IsNullOrEmpty(SelectedTenant) 
                        ? SelectedTenant
                        : _configuration["ApiSettings:DefaultTenant"] ?? "default";
                    
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenantSubdomain }
                    };
                    
                    _logger.LogInformation("Yeni etkinlik oluşturuluyor: {Title}, Tenant: {TenantSubdomain}", model.Title, tenantSubdomain);
                    var response = await _apiService.PostAsyncWithHeaders<EventViewModel>("api/events", model, token, headers);
                    
                    if (response.Success && response.Data != null)
                    {
                        TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    _logger.LogWarning("Etkinlik oluşturulamadı: {Message}", response.Message);
                    TempData["ErrorMessage"] = $"Etkinlik oluşturulamadı: {response.Message}";
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Etkinlik oluşturma form doğrulama hataları: {Errors}", string.Join(", ", errors));
                    TempData["ErrorMessage"] = $"Form doğrulama hataları: {string.Join(", ", errors)}";
                }
                
                // Tenant listesini tekrar yükle
                string tenantToken = await _authService.GetTokenAsync();
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", tenantToken);
                ViewBag.Tenants = tenantsResponse.Success && tenantsResponse.Data != null 
                    ? tenantsResponse.Data 
                    : new List<TenantViewModel>();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                
                // Tenant listesini tekrar yükle
                string tenantToken = await _authService.GetTokenAsync();
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", tenantToken);
                ViewBag.Tenants = tenantsResponse.Success && tenantsResponse.Data != null 
                    ? tenantsResponse.Data 
                    : new List<TenantViewModel>();
                
                return View(model);
            }
        }

        // Etkinlik Katılımcılarını JSON Formatında Getir
        [HttpGet]
        public async Task<IActionResult> GetAttendees(Guid id)
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    return NotFound("Oturum sonlanmış. Lütfen tekrar giriş yapın.");
                }
                
                // Tüm tenantları al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResponse.Success)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {StatusCode} - {Message}", 
                        tenantsResponse.StatusCode, tenantsResponse.Message);
                        
                    string errorDetail = "";
                    if (tenantsResponse.StatusCode == 401)
                    {
                        errorDetail = "Yetkilendirme hatası.";
                    }
                    else if (tenantsResponse.StatusCode == 403)
                    {
                        errorDetail = "Bu işlem için yetkiniz bulunmamaktadır.";
                    }
                    else 
                    {
                        errorDetail = tenantsResponse.Message;
                    }
                    
                    return NotFound($"Tenant listesi alınamadı: {errorDetail}");
                }
                
                if (tenantsResponse.Data == null || !tenantsResponse.Data.Any())
                {
                    _logger.LogWarning("Tenant listesi boş geldi");
                    return NotFound("Tenant listesi boş.");
                }
                
                List<AttendeeViewModel> allAttendees = null;
                
                // Her tenant için etkinliği ara
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} ({TenantId}) için etkinlik aranıyor. X-Tenant: {Subdomain}", 
                        tenant.Name, tenant.Id, tenant.Subdomain);
                    
                    var eventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{id}", token, headers);
                    
                    if (eventResponse.Success && eventResponse.Data != null)
                    {
                        _logger.LogInformation("Etkinlik bulundu: {EventTitle} (ID: {EventId})", 
                            eventResponse.Data.Title, eventResponse.Data.Id);
                        
                        // Doğru tenant bulundu, katılımcıları al
                        var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<AttendeeViewModel>>(
                            $"api/events/{id}/attendees", token, headers);
                        
                        if (attendeesResponse.Success && attendeesResponse.Data != null)
                        {
                            allAttendees = attendeesResponse.Data;
                            _logger.LogInformation("Etkinlik için {Count} katılımcı bulundu", allAttendees.Count);
                            break; // Etkinlik bulundu, döngüden çık
                        }
                        else
                        {
                            _logger.LogWarning("Katılımcılar alınamadı: {StatusCode} - {Message}", 
                                attendeesResponse.StatusCode, attendeesResponse.Message);
                            allAttendees = new List<AttendeeViewModel>();
                            break; // Etkinlik bulundu ama katılımcı yok veya hata var, döngüden çık
                        }
                    }
                }
                
                if (allAttendees != null)
                {
                    return Json(allAttendees);
                }
                
                _logger.LogWarning("Hiçbir tenant'ta ID={Id} olan etkinlik bulunamadı", id);
                return NotFound("Katılımcı listesi bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik katılımcıları JSON formatında getirilirken hata oluştu. EventID: {Id}", id);
                return StatusCode(500, "Sunucu hatası oluştu: " + ex.Message);
            }
        }
    }
} 