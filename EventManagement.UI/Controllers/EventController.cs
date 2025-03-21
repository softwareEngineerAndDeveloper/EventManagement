using EventManagement.UI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using EventManagement.Domain.Entities;
using EventManagement.UI.Models;
using Microsoft.AspNetCore.Http;
using EventManagement.UI.DTOs;

namespace EventManagement.UI.Controllers
{
    public class EventController : Controller
    {
        private readonly IApiServiceUI _apiService;
        private readonly IEventServiceUI _eventService;
        private readonly IAttendeeServiceUI _attendeeService;
        private readonly ILogger<EventController> _logger;

        public EventController(IApiServiceUI apiService, IEventServiceUI eventService, IAttendeeServiceUI attendeeService, ILogger<EventController> logger)
        {
            _apiService = apiService;
            _eventService = eventService;
            _attendeeService = attendeeService;
            _logger = logger;
        }

        private Guid GetTenantId()
        {
            var tenantIdStr = HttpContext.Session.GetString("TenantId");
            
            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out Guid tenantId))
            {
                return tenantId;
            }
            
            // Cookie'den TenantId'yi almayı dene
            if (HttpContext.Request.Cookies.TryGetValue("TenantId", out string cookieTenantIdStr) && 
                !string.IsNullOrEmpty(cookieTenantIdStr) && 
                Guid.TryParse(cookieTenantIdStr, out Guid cookieTenantId))
            {
                // Session'ı da güncelle
                HttpContext.Session.SetString("TenantId", cookieTenantId.ToString());
                return cookieTenantId;
            }
            
            // Varsayılan tenant ID
            var defaultTenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            
            // Session'a kaydet
            HttpContext.Session.SetString("TenantId", defaultTenantId.ToString());
            
            return defaultTenantId;
        }

        // Etkinlik Listeleme
        public async Task<IActionResult> Index(EventFilterDto filter)
        {
            var result = await _apiService.GetAllEventsAsync(filter, GetTenantId());
            
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
            var result = await _apiService.GetEventByIdAsync(id, GetTenantId());
            
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
                // Kullanıcı ID'sini elde etme (önce session, sonra claims)
                Guid userId;
                var userIdString = HttpContext.Session.GetString("UserId");
                
                // Session'dan alınamadıysa Claims'den almayı dene
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out userId))
                {
                    // Claims'den userId almayı dene
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid" || 
                                                                     c.Type == "UserId" || 
                                                                     c.Type == ClaimTypes.NameIdentifier);
                    
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out userId))
                    {
                        // User ID bulunamadı, hata göster
                        ModelState.AddModelError("", "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.");
                        return View(createEventDto);
                    }
                }
                
                // UserId belirlendi, DTO'ya ata
                createEventDto.CreatorId = userId;
                
                // Etkinlik varsayılan olarak aktif ve onaylı olsun
                createEventDto.IsActive = true;
                createEventDto.IsPublic = true;
                
                // Debug için kullanıcı kimliğini loglama
                _logger.LogInformation("Etkinlik oluşturuluyor. Kullanıcı ID: {UserId}", userId);

                // Başlangıç tarihi bitiş tarihinden sonra ise hata göster
                if (createEventDto.StartDate >= createEventDto.EndDate)
                {
                    ModelState.AddModelError("StartDate", "Başlangıç tarihi bitiş tarihinden önce olmalıdır.");
                    return View(createEventDto);
                }

                // Maksimum Katılımcı değeri ayarlanmadıysa, Kapasite değerini kullan
                if (createEventDto.MaxAttendees == null && createEventDto.Capacity.HasValue)
                {
                    createEventDto.MaxAttendees = createEventDto.Capacity;
                }
                
                var result = await _apiService.CreateEventAsync(createEventDto, GetTenantId());
                
                if (result.IsSuccess && result.Data != null)
                {
                    // Etkinliği hemen onaylı duruma getir
                    if (result.Data.Status != Domain.Entities.EventStatus.Approved)
                    {
                        var updateDto = new UpdateEventDto
                        {
                            Id = result.Data.Id,
                            Title = result.Data.Title,
                            Description = result.Data.Description,
                            StartDate = result.Data.StartDate,
                            EndDate = result.Data.EndDate,
                            Location = result.Data.Location,
                            Capacity = result.Data.Capacity,
                            MaxAttendees = result.Data.MaxAttendees,
                            IsActive = true,
                            IsPublic = result.Data.IsPublic,
                            IsCancelled = false,
                            Status = Domain.Entities.EventStatus.Approved // Otomatik olarak onaylı duruma getir
                        };
                        
                        await _apiService.UpdateEventAsync(result.Data.Id, updateDto, GetTenantId());
                        _logger.LogInformation("Etkinlik otomatik olarak onaylı duruma getirildi. EventId: {EventId}", result.Data.Id);
                    }
                    
                    TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu ve onaylandı.";
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
            var result = await _apiService.GetEventByIdAsync(id, GetTenantId());
            
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
                MaxAttendees = eventDto.MaxAttendees,
                IsActive = eventDto.IsActive,
                IsPublic = eventDto.IsPublic,
                IsCancelled = eventDto.IsCancelled,
                Status = eventDto.Status
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
            
            if (!ModelState.IsValid)
            {
                return View(updateEventDto);
            }
            
            try
            {
                var result = await _apiService.UpdateEventAsync(id, updateEventDto, GetTenantId());
                
                if (result.IsSuccess && result.Data != null)
                {
                    TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi.";
                    return RedirectToAction(nameof(Details), new { id = result.Data.Id });
                }
                
                TempData["ErrorMessage"] = result.Message;
                return View(updateEventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik güncellenirken hata. EventId: {EventId}", id);
                ModelState.AddModelError("", "Etkinlik güncellenirken bir hata oluştu: " + ex.Message);
                return View(updateEventDto);
            }
        }

        // Etkinlik Silme Onayı
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _apiService.GetEventByIdAsync(id, GetTenantId());
            
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
            var result = await _apiService.DeleteEventAsync(id, GetTenantId());
            
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
            var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
            var statsResult = await _apiService.GetEventStatisticsAsync(id, GetTenantId());
            
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
            var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
            
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
            
            var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
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
            var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
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

        // Manager Dashboard
        [HttpGet]
        [Authorize(Roles = "EventManager")]
        public async Task<IActionResult> Manager()
        {
            var result = await _apiService.GetAllEventsAsync(new EventFilterDto(), GetTenantId());
            
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(new List<EventDto>());
            }
            
            return View("~/Views/Manager/Index.cshtml", result.Data);
        }

        /// <summary>
        /// Etkinlik katılımcıları sayfasını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Participants(Guid id)
        {
            try
            {
                var eventDto = await _eventService.GetEventByIdAsync(id, GetTenantId());
                if (eventDto == null)
                {
                    return NotFound();
                }
                
                var attendees = await _attendeeService.GetAttendeesByEventIdAsync(id);
                var statistics = await _attendeeService.GetEventStatisticsAsync(id);
                
                var viewModel = new ParticipantViewModel
                {
                    EventId = id,
                    EventTitle = eventDto.Title,
                    Attendees = attendees.Select(a => new ParticipantItemViewModel
                    {
                        Id = a.Id,
                        EventId = a.EventId,
                        Name = a.Name,
                        Email = a.Email,
                        Phone = a.Phone,
                        Status = a.Status,
                        HasAttended = a.HasAttended,
                        RegistrationDate = a.RegistrationDate,
                        Notes = a.Notes,
                        IsCancelled = a.IsCancelled
                    }).ToList(),
                    Statistics = new EventStatisticsViewModel
                    {
                        EventId = statistics.EventId,
                        EventTitle = statistics.EventTitle,
                        TotalAttendees = statistics.TotalAttendees,
                        ConfirmedAttendees = statistics.ConfirmedAttendees,
                        CancelledAttendees = statistics.CancelledAttendees,
                        WaitingListAttendees = statistics.WaitingListAttendees,
                        AvailableCapacity = statistics.AvailableCapacity,
                        MaxCapacity = statistics.MaxCapacity,
                        FillRate = statistics.FillRate
                    }
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcılar görüntülenirken hata oluştu. EventId: {EventId}", id);
                return RedirectToAction("Error", "Home");
            }
        }
        
        /// <summary>
        /// Katılımcı ekleme modalını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddParticipant(Guid eventId)
        {
            var eventDto = await _eventService.GetEventByIdAsync(eventId, GetTenantId());
            if (eventDto == null)
            {
                return NotFound();
            }
            
            ViewBag.EventId = eventId;
            ViewBag.EventTitle = eventDto.Title;
            
            return PartialView("_ParticipantModal", new AttendeeDto { EventId = eventId });
        }
        
        /// <summary>
        /// Yeni katılımcı ekler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddParticipant(AttendeeDto model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Geçersiz form verileri." });
            }
            
            try
            {
                // EventId dolu olduğundan emin olalım
                if (model.EventId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Etkinlik ID gereklidir." });
                }
                
                // Etkinliğin var olduğunu kontrol et
                var eventDto = await _eventService.GetEventByIdAsync(model.EventId, GetTenantId());
                if (eventDto == null)
                {
                    _logger.LogWarning("Etkinlik bulunamadı. EventId: {EventId}", model.EventId);
                    return Json(new { success = false, message = "Etkinlik bulunamadı." });
                }
                
                // Katılımcının e-posta adresine göre zaten var olup olmadığını kontrol et
                var existingAttendees = await _attendeeService.GetAttendeesByEventIdAsync(model.EventId);
                var existingAttendee = existingAttendees.FirstOrDefault(a => a.Email != null && 
                                      a.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                
                if (existingAttendee != null)
                {
                    _logger.LogWarning("Bu e-posta ile kayıtlı katılımcı zaten mevcut. Email: {Email}, EventId: {EventId}", 
                                      model.Email, model.EventId);
                    return Json(new { success = false, message = "Bu e-posta adresi ile kayıtlı bir katılımcı zaten mevcut." });
                }
                
                // Maksimum katılımcı sayısını aşıp aşmadığını kontrol et
                var totalAttendees = existingAttendees.Count(a => !a.IsCancelled);
                if (eventDto.MaxAttendees.HasValue && totalAttendees >= eventDto.MaxAttendees.Value)
                {
                    _logger.LogWarning("Etkinlik maksimum katılımcı sayısına ulaştı. EventId: {EventId}, Mevcut: {Current}, Maximum: {Max}", 
                                      model.EventId, totalAttendees, eventDto.MaxAttendees.Value);
                    return Json(new { success = false, message = "Etkinlik maksimum katılımcı sayısına ulaşmıştır." });
                }
                
                // Etkinliğin aktif olduğunu kontrol et
                if (!eventDto.IsActive || eventDto.IsCancelled || eventDto.Status != Domain.Entities.EventStatus.Approved)
                {
                    _logger.LogWarning("Etkinlik aktif değil. EventId: {EventId}, IsActive: {IsActive}, IsCancelled: {IsCancelled}, Status: {Status}", 
                                      model.EventId, eventDto.IsActive, eventDto.IsCancelled, eventDto.Status);
                    return Json(new { success = false, message = "Etkinlik aktif değil veya iptal edilmiş." });
                }
                
                _logger.LogInformation("Yeni katılımcı ekleniyor. EventId: {EventId}, Email: {Email}", model.EventId, model.Email);
                
                // Model ile doğrudan servis metoduna geçiyoruz
                var result = await _attendeeService.CreateAttendeeAsync(model);
                
                if (result == null)
                {
                    _logger.LogError("Katılımcı eklenirken hata oluştu. EventId: {EventId}, Email: {Email}", model.EventId, model.Email);
                    return Json(new { success = false, message = "Katılımcı eklenirken bir hata oluştu." });
                }
                
                // Etkinlik istatistiklerini güncelle
                var statistics = await _attendeeService.GetEventStatisticsAsync(model.EventId);
                
                _logger.LogInformation("Katılımcı başarıyla eklendi. AttendeeId: {AttendeeId}, EventId: {EventId}", 
                                      result.Id, model.EventId);
                                      
                return Json(new { 
                    success = true, 
                    message = "Katılımcı başarıyla eklendi.",
                    data = result,
                    statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı eklenirken hata oluştu. EventId: {EventId}, Email: {Email}", 
                                model.EventId, model.Email);
                return Json(new { success = false, message = "Katılımcı eklenirken bir hata oluştu: " + ex.Message });
            }
        }
        
        /// <summary>
        /// Katılımcı güncelleme modalını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditParticipant(Guid id)
        {
            try 
            {
                var attendee = await _attendeeService.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    _logger.LogWarning("Düzenlenecek katılımcı bulunamadı. AttendeeId: {AttendeeId}", id);
                    return NotFound();
                }
                
                _logger.LogInformation("Katılımcı düzenleme modalı açılıyor. AttendeeId: {AttendeeId}", id);
                return Json(attendee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı bilgileri alınırken hata oluştu. AttendeeId: {AttendeeId}", id);
                return StatusCode(500, "Katılımcı bilgileri alınırken bir hata oluştu.");
            }
        }
        
        /// <summary>
        /// Katılımcı bilgilerini günceller
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParticipant(AttendeeDto model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Geçersiz form verileri." });
            }
            
            try
            {
                // Id dolu olduğundan emin olalım
                if (model.Id == Guid.Empty)
                {
                    return Json(new { success = false, message = "Katılımcı ID gereklidir." });
                }
                
                // Katılımcının varlığını kontrol et
                var existingAttendee = await _attendeeService.GetAttendeeByIdAsync(model.Id);
                if (existingAttendee == null)
                {
                    _logger.LogWarning("Güncellenecek katılımcı bulunamadı. AttendeeId: {AttendeeId}", model.Id);
                    return Json(new { success = false, message = "Katılımcı bulunamadı." });
                }
                
                // E-posta değiştiyse, aynı etkinlikte aynı e-postayla başka katılımcı var mı kontrol et
                if (!string.Equals(existingAttendee.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingAttendees = await _attendeeService.GetAttendeesByEventIdAsync(model.EventId);
                    var duplicateEmail = existingAttendees.FirstOrDefault(a => 
                        a.Id != model.Id && 
                        a.Email != null && 
                        a.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                    
                    if (duplicateEmail != null)
                    {
                        _logger.LogWarning("Bu e-posta ile kayıtlı başka bir katılımcı mevcut. Email: {Email}, EventId: {EventId}", 
                                          model.Email, model.EventId);
                        return Json(new { success = false, message = "Bu e-posta adresi ile kayıtlı başka bir katılımcı zaten mevcut." });
                    }
                }
                
                // Etkinliğin bilgilerini kontrol et
                var eventDto = await _eventService.GetEventByIdAsync(model.EventId, GetTenantId());
                if (eventDto == null)
                {
                    _logger.LogWarning("Etkinlik bulunamadı. EventId: {EventId}", model.EventId);
                    return Json(new { success = false, message = "Etkinlik bulunamadı." });
                }
                
                // Katılımcıyı güncelle
                _logger.LogInformation("Katılımcı güncelleniyor. AttendeeId: {AttendeeId}, EventId: {EventId}", 
                                      model.Id, model.EventId);
                                      
                var result = await _attendeeService.UpdateAttendeeAsync(model);
                
                if (result == null)
                {
                    _logger.LogError("Katılımcı güncellenirken hata oluştu. AttendeeId: {AttendeeId}", model.Id);
                    return Json(new { success = false, message = "Katılımcı güncellenirken bir hata oluştu." });
                }
                
                // Etkinlik istatistiklerini güncelle
                var statistics = await _attendeeService.GetEventStatisticsAsync(model.EventId);
                
                _logger.LogInformation("Katılımcı başarıyla güncellendi. AttendeeId: {AttendeeId}", model.Id);
                
                return Json(new { 
                    success = true, 
                    message = "Katılımcı başarıyla güncellendi.",
                    data = result,
                    statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı güncellenirken hata oluştu. AttendeeId: {AttendeeId}", model.Id);
                return Json(new { success = false, message = "Katılımcı güncellenirken bir hata oluştu: " + ex.Message });
            }
        }
        
        /// <summary>
        /// Katılımcı siler
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteParticipant(Guid id)
        {
            try
            {
                var attendee = await _attendeeService.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    return Json(new { success = false, message = "Katılımcı bulunamadı." });
                }
                
                var eventId = attendee.EventId;
                var result = await _attendeeService.DeleteAttendeeAsync(id);
                if (!result)
                {
                    return Json(new { success = false, message = "Katılımcı silinirken bir hata oluştu." });
                }
                
                var statistics = await _attendeeService.GetEventStatisticsAsync(eventId);
                
                return Json(new { 
                    success = true, 
                    message = "Katılımcı başarıyla silindi.",
                    statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı silinirken hata oluştu. Id: {Id}", id);
                return Json(new { success = false, message = "Katılımcı silinirken bir hata oluştu." });
            }
        }
        
        /// <summary>
        /// Etkinlik onaylama
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> ApproveEvent(Guid id)
        {
            try
            {
                _logger.LogInformation("Etkinlik onaylama işlemi başlatıldı. EventId: {EventId}", id);
                
                // Etkinliği getir
                var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
                if (!eventResult.IsSuccess || eventResult.Data == null)
                {
                    _logger.LogWarning("Onaylanacak etkinlik bulunamadı. EventId: {EventId}", id);
                    return Json(new { success = false, message = "Etkinlik bulunamadı." });
                }
                
                // Etkinlik zaten onaylıysa bildir
                if (eventResult.Data.Status == Domain.Entities.EventStatus.Approved)
                {
                    _logger.LogInformation("Etkinlik zaten onaylı durumda. EventId: {EventId}", id);
                    return Json(new { success = true, message = "Etkinlik zaten onaylı durumda." });
                }
                
                // Etkinliği güncelle
                var updateDto = new UpdateEventDto
                {
                    Id = eventResult.Data.Id,
                    Title = eventResult.Data.Title,
                    Description = eventResult.Data.Description,
                    StartDate = eventResult.Data.StartDate,
                    EndDate = eventResult.Data.EndDate,
                    Location = eventResult.Data.Location,
                    Capacity = eventResult.Data.Capacity,
                    MaxAttendees = eventResult.Data.MaxAttendees,
                    IsActive = true, // Aktif et
                    IsPublic = eventResult.Data.IsPublic,
                    IsCancelled = false, // İptal edilmişse bile kaldır
                    Status = Domain.Entities.EventStatus.Approved // Onaylı duruma getir
                };
                
                var result = await _apiService.UpdateEventAsync(id, updateDto, GetTenantId());
                
                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("Etkinlik başarıyla onaylandı. EventId: {EventId}", id);
                    return Json(new { success = true, message = "Etkinlik başarıyla onaylandı." });
                }
                else
                {
                    _logger.LogWarning("Etkinlik onaylanırken hata oluştu. EventId: {EventId}, Hata: {Error}", id, result.Message);
                    return Json(new { success = false, message = result.Message ?? "Etkinlik onaylanırken bir hata oluştu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik onaylanırken hata oluştu. EventId: {EventId}", id);
                return Json(new { success = false, message = "Etkinlik onaylanırken bir hata oluştu: " + ex.Message });
            }
        }
        
        /// <summary>
        /// Etkinlik reddetme
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireEventManager")]
        public async Task<IActionResult> RejectEvent(Guid id)
        {
            try
            {
                _logger.LogInformation("Etkinlik reddetme işlemi başlatıldı. EventId: {EventId}", id);
                
                // Etkinliği getir
                var eventResult = await _apiService.GetEventByIdAsync(id, GetTenantId());
                if (!eventResult.IsSuccess || eventResult.Data == null)
                {
                    _logger.LogWarning("Reddedilecek etkinlik bulunamadı. EventId: {EventId}", id);
                    return Json(new { success = false, message = "Etkinlik bulunamadı." });
                }
                
                // Etkinlik zaten reddedilmişse bildir
                if (eventResult.Data.Status == Domain.Entities.EventStatus.Rejected)
                {
                    _logger.LogInformation("Etkinlik zaten reddedilmiş durumda. EventId: {EventId}", id);
                    return Json(new { success = true, message = "Etkinlik zaten reddedilmiş durumda." });
                }
                
                // Etkinliği güncelle
                var updateDto = new UpdateEventDto
                {
                    Id = eventResult.Data.Id,
                    Title = eventResult.Data.Title,
                    Description = eventResult.Data.Description,
                    StartDate = eventResult.Data.StartDate,
                    EndDate = eventResult.Data.EndDate,
                    Location = eventResult.Data.Location,
                    Capacity = eventResult.Data.Capacity,
                    MaxAttendees = eventResult.Data.MaxAttendees,
                    IsActive = false, // Pasif yap
                    IsPublic = eventResult.Data.IsPublic,
                    IsCancelled = true, // İptal et
                    Status = Domain.Entities.EventStatus.Rejected // Reddedilmiş duruma getir
                };
                
                var result = await _apiService.UpdateEventAsync(id, updateDto, GetTenantId());
                
                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("Etkinlik başarıyla reddedildi. EventId: {EventId}", id);
                    return Json(new { success = true, message = "Etkinlik başarıyla reddedildi." });
                }
                else
                {
                    _logger.LogWarning("Etkinlik reddedilirken hata oluştu. EventId: {EventId}, Hata: {Error}", id, result.Message);
                    return Json(new { success = false, message = result.Message ?? "Etkinlik reddedilirken bir hata oluştu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik reddedilirken hata oluştu. EventId: {EventId}", id);
                return Json(new { success = false, message = "Etkinlik reddedilirken bir hata oluştu: " + ex.Message });
            }
        }
        
        /// <summary>
        /// E-posta ile katılımcı arar (otomatik tamamlama için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchParticipantByEmail(string email)
        {
            try
            {
                var attendee = await _attendeeService.SearchAttendeeByEmailAsync(email);
                return Json(new { success = true, data = attendee });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta ile katılımcı aranırken hata oluştu. Email: {Email}", email);
                return Json(new { success = false, message = "Katılımcı aranırken bir hata oluştu." });
            }
        }
        
        /// <summary>
        /// Etkinliğe ait tüm katılımcıları JSON formatında döndürür
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEventParticipants(Guid id)
        {
            try
            {
                var attendees = await _attendeeService.GetAttendeesByEventIdAsync(id);
                return Json(new { success = true, data = attendees });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik katılımcıları alınırken hata oluştu. EventId: {EventId}", id);
                return Json(new { success = false, message = "Katılımcılar alınırken bir hata oluştu." });
            }
        }
        
        // Katılımcı Arama
        [HttpGet]
        public async Task<IActionResult> SearchParticipant(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "E-posta adresi gereklidir" });
            }
            
            try
            {
                // İlk olarak tüm etkinliklerdeki katılımcıları alıp, e-posta adresine göre filtreleme yapacağız
                var filter = new EventFilterDto();
                var eventsResult = await _apiService.GetAllEventsAsync(filter, GetTenantId());
                
                if (!eventsResult.IsSuccess || eventsResult.Data == null)
                {
                    return Json(new { success = false, message = "Etkinlikler alınamadı" });
                }
                
                foreach (var eventItem in eventsResult.Data)
                {
                    var registrationsResult = await _apiService.GetRegistrationsByEventIdAsync(eventItem.Id);
                    
                    if (registrationsResult.IsSuccess && registrationsResult.Data != null)
                    {
                        var participant = registrationsResult.Data.FirstOrDefault(r => 
                            r.ParticipantEmail != null && r.ParticipantEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
                        
                        if (participant != null)
                        {
                            return Json(new { 
                                success = true, 
                                data = new { 
                                    participantName = participant.ParticipantName,
                                    participantEmail = participant.ParticipantEmail,
                                    participantPhone = participant.ParticipantPhone
                                } 
                            });
                        }
                    }
                }
                
                // Katılımcı bulunamadı
                return Json(new { success = false, message = "Bu e-posta adresine ait katılımcı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı arama hatası: {Message}", ex.Message);
                return Json(new { success = false, message = "Katılımcı arama sırasında bir hata oluştu" });
            }
        }
    }
} 