using EventManagement.UI.Models.DTOs;
using EventManagement.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.UI.Controllers
{
    public class EventController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<EventController> _logger;

        public EventController(IApiService apiService, ILogger<EventController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Etkinlik Listeleme
        public async Task<IActionResult> Index(EventFilterDto filter)
        {
            var result = await _apiService.GetAllEventsAsync(filter);
            
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(new List<EventDto>());
            }
            
            return View(result.Data);
        }

        // Etkinlik Detayı
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _apiService.GetEventByIdAsync(id);
            
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            return View(result.Data);
        }

        // Etkinlik Oluşturma Formu
        [Authorize(Policy = "RequireEventManager")]
        public IActionResult Create()
        {
            return View(new CreateEventDto());
        }

        // Etkinlik Oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Create(CreateEventDto createEventDto)
        {
            if (ModelState.IsValid)
            {
                var result = await _apiService.CreateEventAsync(createEventDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Details), new { id = result.Data.Id });
                }
                
                TempData["ErrorMessage"] = result.Message;
            }
            
            return View(createEventDto);
        }

        // Etkinlik Düzenleme Formu
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _apiService.GetEventByIdAsync(id);
            
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            var eventDto = result.Data;
            var updateEventDto = new UpdateEventDto
            {
                Id = eventDto.Id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartDate = eventDto.StartDate,
                EndDate = eventDto.EndDate,
                Location = eventDto.Location,
                Capacity = eventDto.Capacity,
                IsActive = eventDto.IsActive
            };
            
            return View(updateEventDto);
        }

        // Etkinlik Düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Edit(Guid id, UpdateEventDto updateEventDto)
        {
            if (id != updateEventDto.Id)
            {
                TempData["ErrorMessage"] = "Geçersiz etkinlik ID.";
                return RedirectToAction(nameof(Index));
            }
            
            if (ModelState.IsValid)
            {
                var result = await _apiService.UpdateEventAsync(id, updateEventDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi.";
                    return RedirectToAction(nameof(Details), new { id = result.Data.Id });
                }
                
                TempData["ErrorMessage"] = result.Message;
            }
            
            return View(updateEventDto);
        }

        // Etkinlik Silme Onayı
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _apiService.GetEventByIdAsync(id);
            
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            return View(result.Data);
        }

        // Etkinlik Silme
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _apiService.DeleteEventAsync(id);
            
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Etkinlik başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }

        // Etkinlik İstatistikleri
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Statistics(Guid id)
        {
            var eventResult = await _apiService.GetEventByIdAsync(id);
            var statsResult = await _apiService.GetEventStatisticsAsync(id);
            
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                TempData["ErrorMessage"] = eventResult.Message;
                return RedirectToAction(nameof(Index));
            }
            
            if (!statsResult.IsSuccess || statsResult.Data == null)
            {
                TempData["ErrorMessage"] = statsResult.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            
            ViewData["EventTitle"] = eventResult.Data.Title;
            return View(statsResult.Data);
        }

        // Etkinliğe Kayıt Ol
        public async Task<IActionResult> Register(Guid id)
        {
            var eventResult = await _apiService.GetEventByIdAsync(id);
            
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                TempData["ErrorMessage"] = eventResult.Message;
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["EventTitle"] = eventResult.Data.Title;
            ViewData["EventId"] = id;
            
            return View(new CreateRegistrationDto { EventId = id });
        }

        // Etkinliğe Kayıt Ol (Form Gönderimi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Guid id, CreateRegistrationDto createRegistrationDto)
        {
            if (id != createRegistrationDto.EventId)
            {
                TempData["ErrorMessage"] = "Geçersiz etkinlik ID.";
                return RedirectToAction(nameof(Index));
            }
            
            if (ModelState.IsValid)
            {
                var result = await _apiService.CreateRegistrationAsync(id, createRegistrationDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    TempData["SuccessMessage"] = "Etkinliğe başarıyla kaydoldunuz.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                TempData["ErrorMessage"] = result.Message;
            }
            
            var eventResult = await _apiService.GetEventByIdAsync(id);
            if (eventResult.IsSuccess && eventResult.Data != null)
            {
                ViewData["EventTitle"] = eventResult.Data.Title;
            }
            
            ViewData["EventId"] = id;
            return View(createRegistrationDto);
        }

        // Etkinlik Kayıtlarını Listele
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Registrations(Guid id)
        {
            var eventResult = await _apiService.GetEventByIdAsync(id);
            var registrationsResult = await _apiService.GetRegistrationsByEventIdAsync(id);
            
            if (!eventResult.IsSuccess || eventResult.Data == null)
            {
                TempData["ErrorMessage"] = eventResult.Message;
                return RedirectToAction(nameof(Index));
            }
            
            if (!registrationsResult.IsSuccess)
            {
                TempData["ErrorMessage"] = registrationsResult.Message;
                registrationsResult.Data = new List<RegistrationDto>();
            }
            
            ViewData["EventTitle"] = eventResult.Data.Title;
            ViewData["EventId"] = id;
            
            return View(registrationsResult.Data);
        }
    }
} 