using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventManagement.UI.Models.Event;
using System.Security.Claims;
using System.Diagnostics;
using System.Text.Json;
using EventManagement.UI.Models.Attendee;

namespace EventManagement.UI.Areas.EventManager.Controllers
{
    [Area("EventManager")]
    [Authorize(Roles = "EventManager")]
    public class EventController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<EventController> _logger;

        public EventController(IApiService apiService, IAuthService authService, ILogger<EventController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Index: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.GetAsync<List<EventViewModel>>("api/events", token);

                if (response.Success)
                {
                    return View(response.Data);
                }

                TempData["ErrorMessage"] = response.Message;
                return View(new List<EventViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlikler listelenirken hata oluştu");
                TempData["ErrorMessage"] = "Etkinlikler listelenirken beklenmeyen bir hata oluştu.";
                return View(new List<EventViewModel>());
            }
        }

        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Details: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.GetAsync<EventViewModel>($"api/events/{id}", token);

                if (response.Success)
                {
                    return View(response.Data);
                }

                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik detayları görüntülenirken hata oluştu, ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik detayları görüntülenirken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Create POST: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş! Oturum sonlanmış olabilir.");
                    TempData["ErrorMessage"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Kullanıcı kimliği alınamadı!");
                    TempData["ErrorMessage"] = "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }
                
                var apiModel = new Dictionary<string, object>
                {
                    { "title", model.Title },
                    { "description", model.Description },
                    { "startDate", model.StartDate },
                    { "endDate", model.EndDate },
                    { "location", model.Location },
                    { "maxAttendees", model.MaxAttendees ?? 0 },
                    { "isPublic", model.IsPublic },
                    { "creatorId", Guid.Parse(userId) }
                };
                
                _logger.LogInformation("Etkinlik oluşturuluyor, CreatorId: {0}", userId);
                
                var response = await _apiService.PostAsync<EventViewModel>("api/events", apiModel, token);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Etkinlik oluşturulurken beklenmeyen bir hata oluştu.";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Edit GET: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.GetAsync<EventViewModel>($"api/events/{id}", token);

                if (response.Success)
                {
                    var editModel = new UpdateEventViewModel
                    {
                        Id = response.Data.Id,
                        Title = response.Data.Title,
                        Description = response.Data.Description,
                        StartDate = response.Data.StartDate,
                        EndDate = response.Data.EndDate,
                        Location = response.Data.Location,
                        MaxAttendees = response.Data.MaxAttendees,
                        IsPublic = response.Data.IsPublic
                    };

                    return View(editModel);
                }

                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik düzenleme sayfası açılırken hata oluştu, ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik düzenleme sayfası açılırken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateEventViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Geçersiz etkinlik ID'si.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Edit POST: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.PutAsync<EventViewModel>($"api/events/{id}", model, token);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = response.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik güncellenirken hata oluştu, ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik güncellenirken beklenmeyen bir hata oluştu.";
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Delete GET: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.GetAsync<EventViewModel>($"api/events/{id}", token);

                if (response.Success)
                {
                    return View(response.Data);
                }

                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik silme sayfası açılırken hata oluştu, ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik silme sayfası açılırken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _logger.LogInformation("Delete POST: Token alındı: {0}", !string.IsNullOrEmpty(token));
                
                var response = await _apiService.DeleteAsync<bool>($"api/events/{id}", token);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla silindi.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik silinirken hata oluştu, ID: {Id}", id);
                TempData["ErrorMessage"] = "Etkinlik silinirken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendees(Guid id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                
                var response = await _apiService.GetAsync<List<AttendeeViewModel>>($"api/events/{id}/attendees", token);
                
                if (response.Success)
                {
                    return Json(new { success = true, data = response.Data });
                }
                
                return Json(new { success = false, message = response.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik katılımcıları getirilirken hata oluştu, ID: {Id}", id);
                return Json(new { success = false, message = "Katılımcılar yüklenirken beklenmeyen bir hata oluştu." });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendee(Guid eventId, AddAttendeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new 
                { 
                    success = false, 
                    message = "Geçersiz form verileri.", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList() 
                });
            }
            
            try
            {
                var token = await _authService.GetTokenAsync();
                
                // API'ye gönderilecek modeli oluştur - CreateAttendeeDto yapısına uygun olarak
                var apiModel = new
                {
                    eventId = eventId,
                    name = model.FullName, // fullName yerine name kullan
                    email = model.Email,
                    phone = model.Phone
                };
                
                _logger.LogInformation("Katılımcı eklenirken API'ye gönderilen model: {@ApiModel}", apiModel);
                
                var response = await _apiService.PostAsync<AttendeeViewModel>($"api/events/{eventId}/attendees", apiModel, token);
                
                if (response.Success)
                {
                    return Json(new { success = true, data = response.Data, message = "Katılımcı başarıyla eklendi." });
                }
                
                return Json(new { success = false, message = response.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinliğe katılımcı eklenirken hata oluştu, ID: {EventId}", eventId);
                return Json(new { success = false, message = "Katılımcı eklenirken beklenmeyen bir hata oluştu." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new EventManagement.UI.Models.Shared.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 