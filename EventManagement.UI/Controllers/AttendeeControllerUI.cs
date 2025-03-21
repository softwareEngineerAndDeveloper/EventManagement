using EventManagement.UI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EventManagement.UI.DTOs;

namespace EventManagement.UI.Controllers
{
    [Authorize]
    public class AttendeeControllerUI : Controller
    {
        private readonly IAttendeeServiceUI _attendeeService;
        private readonly IEventServiceUI _eventService;
        private readonly ILogger<AttendeeControllerUI> _logger;

        public AttendeeControllerUI(
            IAttendeeServiceUI attendeeService, 
            IEventServiceUI eventService,
            ILogger<AttendeeControllerUI> logger)
        {
            _attendeeService = attendeeService;
            _eventService = eventService;
            _logger = logger;
        }

        // API tarafından çağırılan ve etkinliğe katılımcı getiren endpoint
        [HttpGet("api/events/{eventId}/attendees")]
        public async Task<IActionResult> GetAttendeesByEventId(Guid eventId)
        {
            try
            {
                var attendees = await _attendeeService.GetAttendeesByEventIdAsync(eventId);
                return Json(new { success = true, data = attendees });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik katılımcıları getirilirken hata oluştu. EventId: {EventId}", eventId);
                return Json(new { success = false, message = "Katılımcılar getirilirken bir hata oluştu." });
            }
        }

        // Katılımcı arama
        [HttpGet("api/events/{eventId}/attendees/search")]
        public async Task<IActionResult> SearchAttendees(Guid eventId, string searchTerm = null, int? status = null)
        {
            try
            {
                var attendees = await _attendeeService.SearchAttendeesAsync(eventId, searchTerm, status);
                return Json(new { success = true, data = attendees });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı arama sırasında hata oluştu. EventId: {EventId}, SearchTerm: {SearchTerm}, Status: {Status}", 
                    eventId, searchTerm, status);
                return Json(new { success = false, message = "Katılımcı arama sırasında bir hata oluştu." });
            }
        }

        // Etkinlik istatistikleri
        [HttpGet("api/events/{eventId}/statistics")]
        public async Task<IActionResult> GetEventStatistics(Guid eventId)
        {
            try
            {
                var statistics = await _attendeeService.GetEventStatisticsAsync(eventId);
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik istatistikleri getirilirken hata oluştu. EventId: {EventId}", eventId);
                return Json(new { success = false, message = "Etkinlik istatistikleri getirilirken bir hata oluştu." });
            }
        }

        // Katılımcı detayları
        [HttpGet("api/attendees/{id}")]
        public async Task<IActionResult> GetAttendeeById(Guid id)
        {
            try
            {
                var attendee = await _attendeeService.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    return NotFound(new { success = false, message = "Katılımcı bulunamadı." });
                }
                return Json(new { success = true, data = attendee });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı detayları getirilirken hata oluştu. Id: {Id}", id);
                return Json(new { success = false, message = "Katılımcı detayları getirilirken bir hata oluştu." });
            }
        }

        // Yeni katılımcı ekleme
        [HttpPost("api/attendees")]
        public async Task<IActionResult> CreateAttendee([FromBody] AttendeeDto attendeeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdAttendee = await _attendeeService.CreateAttendeeAsync(attendeeDto);
                if (createdAttendee == null)
                {
                    return BadRequest(new { success = false, message = "Katılımcı oluşturulurken bir hata oluştu." });
                }

                var statistics = await _attendeeService.GetEventStatisticsAsync(attendeeDto.EventId);
                return Json(new { 
                    success = true, 
                    message = "Katılımcı başarıyla eklendi.", 
                    data = createdAttendee,
                    statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı oluşturulurken hata oluştu. EventId: {EventId}", attendeeDto.EventId);
                return Json(new { success = false, message = "Katılımcı oluşturulurken bir hata oluştu." });
            }
        }

        // Katılımcı güncelleme
        [HttpPut("api/attendees/{id}")]
        public async Task<IActionResult> UpdateAttendee(Guid id, [FromBody] AttendeeDto attendeeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != attendeeDto.Id)
            {
                return BadRequest(new { success = false, message = "Parametre ID'si ile Katılımcı ID'si eşleşmiyor." });
            }

            try
            {
                var updatedAttendee = await _attendeeService.UpdateAttendeeAsync(attendeeDto);
                if (updatedAttendee == null)
                {
                    return BadRequest(new { success = false, message = "Katılımcı güncellenirken bir hata oluştu." });
                }

                var statistics = await _attendeeService.GetEventStatisticsAsync(attendeeDto.EventId);
                return Json(new { 
                    success = true, 
                    message = "Katılımcı başarıyla güncellendi.", 
                    data = updatedAttendee,
                    statistics = statistics 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı güncellenirken hata oluştu. Id: {Id}", id);
                return Json(new { success = false, message = "Katılımcı güncellenirken bir hata oluştu." });
            }
        }

        // Katılımcı silme
        [HttpDelete("api/attendees/{id}")]
        public async Task<IActionResult> DeleteAttendee(Guid id)
        {
            try
            {
                var attendee = await _attendeeService.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    return NotFound(new { success = false, message = "Katılımcı bulunamadı." });
                }

                var eventId = attendee.EventId;
                bool result = await _attendeeService.DeleteAttendeeAsync(id);
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Katılımcı silinirken bir hata oluştu." });
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
    }
} 