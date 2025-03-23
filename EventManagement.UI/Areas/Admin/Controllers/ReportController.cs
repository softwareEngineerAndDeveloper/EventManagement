using EventManagement.UI.Models.Event;
using EventManagement.UI.Models.Report;
using EventManagement.UI.Models.Tenant;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IApiService apiService, ILogger<ReportController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                string token = null;
                if (HttpContext.Session.Keys.Contains("AuthToken"))
                {
                    token = HttpContext.Session.GetString("AuthToken");
                }

                // Tüm etkinlikleri al (dropdown için)
                // Önce tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                if (!tenantsResponse.Success || tenantsResponse.Data == null)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {Message}", tenantsResponse.Message);
                    ViewBag.ErrorMessage = "Kiracı listesi alınamadı.";
                    return View(new DashboardReportViewModel
                    {
                        UpcomingEvents = new UpcomingEventsReportViewModel { Events = new List<UpcomingEventViewModel>() },
                        EventStatistics = null
                    });
                }

                // Tüm tenant'lar için etkinlikleri topla
                var events = new List<EventViewModel>();
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    _logger.LogInformation("Tenant {TenantName} için etkinlikler alınıyor...", tenant.Name);
                    
                    var eventsResponse = await _apiService.GetAsyncWithHeaders<List<EventViewModel>>("api/events", token, headers);
                    if (eventsResponse.Success && eventsResponse.Data != null)
                    {
                        // Tenant bilgisini etkinliklere ekle
                        foreach (var eventItem in eventsResponse.Data)
                        {
                            eventItem.TenantName = tenant.Name;
                            eventItem.TenantSubdomain = tenant.Subdomain;
                            events.Add(eventItem);
                        }
                        
                        _logger.LogInformation("Tenant {TenantName} için {Count} etkinlik bulundu", 
                            tenant.Name, eventsResponse.Data.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Tenant {TenantName} için etkinlik bulunamadı: {Message}", 
                            tenant.Name, eventsResponse.Message);
                    }
                }
                
                // Tüm etkinliklerin katılımcı sayılarını al
                int totalParticipants = 0;
                int confirmedParticipants = 0;
                
                foreach (var eventItem in events)
                {
                    try 
                    {
                        var attendeesResponse = await _apiService.GetAsync<List<Models.Attendee.AttendeeViewModel>>($"api/events/{eventItem.Id}/attendees", token);
                        if (attendeesResponse.Success && attendeesResponse.Data != null)
                        {
                            var attendees = attendeesResponse.Data;
                            eventItem.RegistrationCount = attendees.Count;
                            totalParticipants += attendees.Count;
                            confirmedParticipants += attendees.Count(a => a.Status == Models.Attendee.AttendeeStatus.Confirmed && a.HasAttended);
                            
                            _logger.LogInformation("Etkinlik {EventId} için katılımcı sayısı: {Count}", eventItem.Id, attendees.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Etkinlik {EventId} için katılımcı sayısı alınırken hata oluştu", eventItem.Id);
                    }
                }

                // Yaklaşan etkinlikler raporunu al
                var upcomingEventsResponse = await _apiService.GetAsync<List<UpcomingEventViewModel>>("api/reports/upcoming-events", token);
                var upcomingEvents = upcomingEventsResponse.Success && upcomingEventsResponse.Data != null
                    ? upcomingEventsResponse.Data
                    : new List<UpcomingEventViewModel>();

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

                // İstatistik için varsayılan etkinlik (eğer varsa)
                EventStatisticsViewModel eventStatistics = null;
                if (events.Count > 0)
                {
                    var defaultEventId = events[0].Id;
                    // Direkt statistics endpoint'i yerine manuel olarak hesaplama yap
                    eventStatistics = await GetCalculatedEventStatistics(defaultEventId, token);
                }

                // Etkinlikler dropdown'ı için SelectList oluştur
                var eventsList = new SelectList(events, "Id", "Title");
                ViewBag.Events = eventsList;

                var model = new DashboardReportViewModel
                {
                    UpcomingEvents = new UpcomingEventsReportViewModel 
                    { 
                        Events = upcomingEvents, 
                        TotalEvents = upcomingEvents.Count,
                        GeneratedAt = DateTime.Now
                    },
                    EventStatistics = eventStatistics
                };
                
                // Özet kartlar için verileri ViewBag'e ekle
                ViewBag.TotalParticipants = totalParticipants;
                ViewBag.ConfirmedParticipants = confirmedParticipants;
                decimal attendanceRate = totalParticipants > 0 ? Math.Round((decimal)confirmedParticipants / totalParticipants * 100, 1) : 0;
                ViewBag.AttendanceRate = attendanceRate;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raporlar yüklenirken hata oluştu");
                TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                return View(new DashboardReportViewModel
                {
                    UpcomingEvents = new UpcomingEventsReportViewModel { Events = new List<UpcomingEventViewModel>() },
                    EventStatistics = null
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEventStatistics(Guid id)
        {
            try
            {
                string token = null;
                if (HttpContext.Session.Keys.Contains("AuthToken"))
                {
                    token = HttpContext.Session.GetString("AuthToken");
                }

                _logger.LogInformation("Etkinlik istatistik isteği alındı. EventID: {EventId}", id);

                // Manuel olarak hesaplanmış istatistikleri al
                var statistics = await GetCalculatedEventStatistics(id, token);
                if (statistics != null)
                {
                    _logger.LogInformation("Etkinlik istatistikleri hesaplandı ve döndürülüyor. EventID: {EventId}", id);
                    return Json(statistics);
                }

                _logger.LogWarning("Etkinlik istatistikleri hesaplanamadı. EventID: {EventId}", id);
                return Json(new { success = false, message = "Etkinlik istatistikleri hesaplanamadı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik istatistikleri alınırken hata oluştu. EventID: {EventId}", id);
                return Json(new { success = false, message = "Etkinlik istatistikleri alınamadı." });
            }
        }
        
        // Manuel istatistik hesaplama
        private async Task<EventStatisticsViewModel> GetCalculatedEventStatistics(Guid eventId, string token)
        {
            try
            {
                // Tenant listesini al
                var tenantsResponse = await _apiService.GetAsync<List<TenantViewModel>>("api/tenants", token);
                if (!tenantsResponse.Success || tenantsResponse.Data == null)
                {
                    _logger.LogWarning("İstatistikler için tenant listesi alınamadı: {Message}", tenantsResponse.Message);
                    return null;
                }
                
                // Her tenant için etkinliği ara
                foreach (var tenant in tenantsResponse.Data)
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "X-Tenant", tenant.Subdomain }
                    };
                    
                    // Etkinlik detaylarını al
                    var eventResponse = await _apiService.GetAsyncWithHeaders<EventViewModel>($"api/events/{eventId}", token, headers);
                    if (!eventResponse.Success || eventResponse.Data == null)
                    {
                        continue; // Bu tenant'da etkinlik yoksa diğerine geç
                    }
                    
                    var eventDetails = eventResponse.Data;
                    eventDetails.TenantName = tenant.Name;
                    eventDetails.TenantSubdomain = tenant.Subdomain;
                    
                    // Katılımcıları al
                    var attendeesResponse = await _apiService.GetAsyncWithHeaders<List<Models.Attendee.AttendeeViewModel>>($"api/events/{eventId}/attendees", token, headers);
                    var attendees = attendeesResponse.Success ? attendeesResponse.Data ?? new List<Models.Attendee.AttendeeViewModel>() : new List<Models.Attendee.AttendeeViewModel>();
                    
                    // İstatistik nesnesini oluştur
                    var statistics = new EventStatisticsViewModel
                    {
                        EventId = eventId,
                        EventTitle = eventDetails.Title,
                        TenantName = tenant.Name,
                        EventDate = eventDetails.StartDate,
                        TotalRegistrations = attendees.Count,
                        ConfirmedAttendees = attendees.Count(a => a.Status == Models.Attendee.AttendeeStatus.Confirmed),
                        CancelledRegistrations = attendees.Count(a => a.Status == Models.Attendee.AttendeeStatus.Cancelled),
                        PendingRegistrations = attendees.Count(a => a.Status == Models.Attendee.AttendeeStatus.Pending),
                        ActualAttendees = attendees.Count(a => a.HasAttended)
                    };
                    
                    // Günlük kayıtları hesapla
                    var registrationsByDay = attendees
                        .GroupBy(a => a.RegistrationDate.Date)
                        .Select(g => new RegistrationsByDayViewModel
                        {
                            Date = g.Key,
                            Count = g.Count()
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                    
                    statistics.RegistrationsByDay = registrationsByDay;
                    
                    // Durum bazlı kayıtları hesapla
                    statistics.RegistrationsByStatus = new List<RegistrationsByStatusViewModel>
                    {
                        new RegistrationsByStatusViewModel 
                        { 
                            Status = "Onaylandı", 
                            Count = statistics.ConfirmedAttendees,
                            Color = "#28a745"
                        },
                        new RegistrationsByStatusViewModel 
                        { 
                            Status = "Beklemede", 
                            Count = statistics.PendingRegistrations,
                            Color = "#ffc107"
                        },
                        new RegistrationsByStatusViewModel 
                        { 
                            Status = "İptal Edildi", 
                            Count = statistics.CancelledRegistrations,
                            Color = "#dc3545"
                        }
                    };
                    
                    return statistics; // İlk bulunan etkinlik için istatistikleri döndür
                }
                
                // Hiçbir tenant'da etkinlik bulunamadı
                _logger.LogWarning("Hiçbir tenant'da ID={Id} olan etkinlik bulunamadı", eventId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik istatistikleri hesaplanırken hata oluştu. EventID: {EventId}", eventId);
                return null;
            }
        }
    }
} 