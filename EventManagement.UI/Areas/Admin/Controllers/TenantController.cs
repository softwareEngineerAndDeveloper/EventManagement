using EventManagement.UI.Models.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using EventManagement.UI.Services.Interfaces;
using EventManagement.UI.Models.Shared;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TenantController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<TenantController> _logger;
        private readonly IConfiguration _configuration;

        public TenantController(IApiService apiService, ILogger<TenantController> logger, IConfiguration configuration)
        {
            _apiService = apiService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Auth token'ını session'dan al
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Session'da auth token bulunamadı. Kullanıcının giriş yapması gerekiyor.");
                    TempData["ErrorMessage"] = "Oturumunuz sona ermiş veya doğru şekilde oturum açmamışsınız. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                
                _logger.LogInformation("Auth token session'dan alındı: {tokenStart}...", 
                    token.Substring(0, Math.Min(token.Length, 20)));

                // Önce ApiService ile dene (token ile)
                var endpoint = _configuration["ApiSettings:Endpoints:Tenants"] ?? "api/tenants";
                var response = await _apiService.GetAsync<List<TenantViewModel>>(endpoint, token);

                if (response.Success)
                {
                    _logger.LogInformation("ApiService ile başarılı yanıt alındı: {count} tenant", 
                        response.Data?.Count.ToString() ?? "0");
                    return View(response.Data);
                }
                
                // ApiService başarısız olduysa doğrudan HttpClient ile dene
                _logger.LogWarning("ApiService başarısız oldu, HttpClientFactory ile deneniyor... Hata: {message}", response.Message);
                var client = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
                
                // İstek detaylarını logla
                var baseUrl = client.BaseAddress?.ToString() ?? string.Empty;
                _logger.LogInformation("API URL: {baseUrl}{endpoint}", baseUrl, endpoint);
                
                // X-Tenant header'ını kontrol et ve ekle
                string? tenantHeader = null;
                if (client.DefaultRequestHeaders.TryGetValues("X-Tenant", out var tenantValues))
                {
                    tenantHeader = tenantValues.FirstOrDefault();
                }
                
                _logger.LogInformation("X-Tenant header: {tenantHeader}", tenantHeader ?? "bulunamadı");
                
                if (string.IsNullOrEmpty(tenantHeader))
                {
                    var defaultTenant = _configuration["ApiSettings:DefaultTenant"];
                    if (!string.IsNullOrEmpty(defaultTenant))
                    {
                        client.DefaultRequestHeaders.Add("X-Tenant", defaultTenant);
                        _logger.LogInformation("X-Tenant header eklendi: {defaultTenant}", defaultTenant);
                    }
                }
                
                // Token ile Authorization header ekle
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("Bearer token Authorization header'a eklendi");
                
                var directResponse = await client.GetAsync(endpoint);
                
                if (!directResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API yanıtı başarısız: HTTP {statusCode}", directResponse.StatusCode);
                    TempData["ErrorMessage"] = $"Tenant bilgileri alınamadı. Durum kodu: {directResponse.StatusCode}";
                    return View(new List<TenantViewModel>());
                }
                
                directResponse.EnsureSuccessStatusCode();
                
                var content = await directResponse.Content.ReadAsStringAsync();
                var contentSummary = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                _logger.LogInformation("API yanıtı: {content}", contentSummary);
                
                // Yanıtı JObject olarak parse et
                var responseObj = JObject.Parse(content);
                
                bool isSuccess = responseObj["isSuccess"]?.Value<bool>() ?? false;
                if (isSuccess && responseObj["data"] != null)
                {
                    var tenantsJson = responseObj["data"].ToString();
                    var tenants = JsonConvert.DeserializeObject<List<TenantViewModel>>(tenantsJson);
                    
                    if (tenants != null)
                    {
                        _logger.LogInformation("Doğrudan HTTP ile {count} tenant alındı", tenants.Count.ToString());
                        return View(tenants);
                    }
                }
                
                _logger.LogWarning("API yanıtı başarısız: {Message}", response.Message);
                TempData["ErrorMessage"] = "Tenant bilgileri alınamadı: " + response.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant listesi alınırken hata oluştu");
                TempData["ErrorMessage"] = "Tenant bilgileri alınırken bir hata oluştu: " + ex.Message;
            }

            return View(new List<TenantViewModel>());
        }

        public IActionResult Create()
        {
            return View(new TenantViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Auth token'ını session'dan al
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Session'da auth token bulunamadı. Kullanıcının giriş yapması gerekiyor.");
                    TempData["ErrorMessage"] = "Oturumunuz sona ermiş veya doğru şekilde oturum açmamışsınız. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var endpoint = _configuration["ApiSettings:Endpoints:Tenants"] ?? "api/tenants";
                var createModel = new
                {
                    Name = model.Name,
                    Description = model.Description,
                    Subdomain = model.Subdomain,
                    ContactEmail = model.ContactEmail,
                    ContactPhone = model.ContactPhone
                };

                var response = await _apiService.PostAsync<TenantViewModel>(endpoint, createModel, token);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Tenant başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Tenant oluşturma API yanıtı başarısız: {Message}", response.Message);
                    TempData["ErrorMessage"] = "Tenant oluşturulamadı: " + response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Tenant oluşturulurken bir hata oluştu: " + ex.Message;
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // Auth token'ını session'dan al
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Session'da auth token bulunamadı. Kullanıcının giriş yapması gerekiyor.");
                    TempData["ErrorMessage"] = "Oturumunuz sona ermiş veya doğru şekilde oturum açmamışsınız. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var endpoint = $"{_configuration["ApiSettings:Endpoints:Tenants"]}/{id}" ?? $"api/tenants/{id}";
                var response = await _apiService.GetAsync<TenantViewModel>(endpoint, token);

                if (response.Success && response.Data != null)
                {
                    return View(response.Data);
                }
                else
                {
                    _logger.LogWarning("Tenant detayı API yanıtı başarısız: {Message}", response.Message);
                    TempData["ErrorMessage"] = "Tenant detayları alınamadı: " + response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant detayları alınırken hata oluştu");
                TempData["ErrorMessage"] = "Tenant detayları alınırken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TenantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Auth token'ını session'dan al
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Session'da auth token bulunamadı. Kullanıcının giriş yapması gerekiyor.");
                    TempData["ErrorMessage"] = "Oturumunuz sona ermiş veya doğru şekilde oturum açmamışsınız. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var endpoint = $"{_configuration["ApiSettings:Endpoints:Tenants"]}/{model.Id}" ?? $"api/tenants/{model.Id}";
                var updateModel = new
                {
                    Name = model.Name,
                    Description = model.Description,
                    ContactEmail = model.ContactEmail,
                    ContactPhone = model.ContactPhone,
                    IsActive = model.IsActive
                };

                var response = await _apiService.PutAsync<TenantViewModel>(endpoint, updateModel, token);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Tenant başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Tenant güncelleme API yanıtı başarısız: {Message}", response.Message);
                    TempData["ErrorMessage"] = "Tenant güncellenemedi: " + response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant güncellenirken hata oluştu");
                TempData["ErrorMessage"] = "Tenant güncellenirken bir hata oluştu: " + ex.Message;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            try
            {
                // Auth token'ını session'dan al
                var token = HttpContext.Session.GetString("AuthToken");
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Session'da auth token bulunamadı. Kullanıcının giriş yapması gerekiyor.");
                    TempData["ErrorMessage"] = "Oturumunuz sona ermiş veya doğru şekilde oturum açmamışsınız. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var endpoint = $"{_configuration["ApiSettings:Endpoints:Tenants"]}/current" ?? "api/tenants/current";
                var response = await _apiService.GetAsync<TenantViewModel>(endpoint, token);

                if (response.Success && response.Data != null)
                {
                    return View(response.Data);
                }
                else
                {
                    _logger.LogWarning("Mevcut tenant API yanıtı başarısız: {Message}", response.Message);
                    TempData["ErrorMessage"] = "Mevcut tenant bilgileri alınamadı: " + response.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mevcut tenant bilgileri alınırken hata oluştu");
                TempData["ErrorMessage"] = "Mevcut tenant bilgileri alınırken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 