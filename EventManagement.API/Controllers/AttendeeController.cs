using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagement.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class AttendeeController : ControllerBase
    {
        private readonly IAttendeeService _attendeeService;
        private readonly ILogger<AttendeeController> _logger;

        public AttendeeController(IAttendeeService attendeeService, ILogger<AttendeeController> logger)
        {
            _attendeeService = attendeeService;
            _logger = logger;
        }

        /// <summary>
        /// Etkinliğe ait tüm katılımcıları getirir
        /// </summary>
        [HttpGet("{eventId}/attendees")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> GetAttendeesByEventId(Guid eventId)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                // Admin rolü kontrolü
                bool isAdmin = User.IsInRole("Admin");
                
                var response = await _attendeeService.GetAttendeesByEventIdAsync(eventId, parsedTenantId, isAdmin);
                
                if (!response.IsSuccess)
                {
                    if (response.Message.Contains("bulunamadı"))
                    {
                        return NotFound(new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = response.Message });
                    }
                    
                    if (response.Message.Contains("erişim izniniz yok"))
                    {
                        return Forbid();
                    }
                    
                    return BadRequest(new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = response.Message });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcılar getirilirken bir hata oluştu. EventId: {EventId}", eventId);
                return StatusCode(500, new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = "Katılımcılar getirilirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// ID'ye göre katılımcı bilgilerini getirir
        /// </summary>
        [HttpGet("attendees/{id}")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> GetAttendeeById(Guid id)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.GetAttendeeByIdAsync(id, parsedTenantId);
                
                if (!response.IsSuccess)
                {
                    return NotFound(response.Message);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı getirilirken bir hata oluştu. Id: {Id}", id);
                return StatusCode(500, "Katılımcı getirilirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Yeni bir katılımcı oluşturur
        /// </summary>
        [HttpPost("{eventId}/attendees")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> CreateAttendee(Guid eventId, [FromBody] CreateAttendeeDto createAttendeeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // URL'deki eventId ile DTO'daki eventId uyuşmalı
                if (eventId != createAttendeeDto.EventId)
                {
                    return BadRequest("URL'deki etkinlik ID'si ile istekteki etkinlik ID'si uyuşmuyor");
                }

                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.CreateAttendeeAsync(createAttendeeDto, parsedTenantId);
                
                if (!response.IsSuccess)
                {
                    return BadRequest(response.Message);
                }

                return CreatedAtAction(nameof(GetAttendeeById), new { id = response.Data.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı eklenirken bir hata oluştu. EventId: {EventId}", eventId);
                return StatusCode(500, "Katılımcı eklenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Katılımcı bilgilerini günceller
        /// </summary>
        [HttpPut("attendees/{id}")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> UpdateAttendee(Guid id, [FromBody] UpdateAttendeeDto updateAttendeeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.UpdateAttendeeAsync(id, updateAttendeeDto, parsedTenantId);
                
                if (!response.IsSuccess)
                {
                    return BadRequest(response.Message);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı güncellenirken bir hata oluştu. Id: {Id}", id);
                return StatusCode(500, "Katılımcı güncellenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Katılımcıyı siler
        /// </summary>
        [HttpDelete("attendees/{id}")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> DeleteAttendee(Guid id)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.DeleteAttendeeAsync(id, parsedTenantId);
                
                if (!response.IsSuccess)
                {
                    return NotFound(response.Message);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı silinirken bir hata oluştu. Id: {Id}", id);
                return StatusCode(500, "Katılımcı silinirken bir hata oluştu");
            }
        }

        /// <summary>
        /// E-posta adresine göre katılımcı arar
        /// </summary>
        [HttpGet("attendees/search")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> SearchAttendeeByEmail([FromQuery] string email)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.SearchAttendeeByEmailAsync(email, parsedTenantId);
                
                if (!response.IsSuccess)
                {
                    return NotFound(response.Message);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı aranırken bir hata oluştu. Email: {Email}", email);
                return StatusCode(500, "Katılımcı aranırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Belirli kriterlere göre katılımcı arar
        /// </summary>
        [HttpGet("attendees")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> SearchAttendees([FromQuery] SearchAttendeeDto searchAttendeeDto)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                var response = await _attendeeService.SearchAttendeesAsync(searchAttendeeDto, parsedTenantId);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcılar aranırken bir hata oluştu");
                return StatusCode(500, "Katılımcılar aranırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Etkinlik istatistiklerini getirir
        /// </summary>
        [HttpGet("{eventId}/attendee-statistics")]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> GetEventStatistics(Guid eventId)
        {
            try
            {
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    return BadRequest("Geçersiz tenant bilgisi");
                }

                // Admin rolü kontrolü
                bool isAdmin = User.IsInRole("Admin");
                
                var response = await _attendeeService.GetEventStatisticsAsync(eventId, parsedTenantId, isAdmin);
                
                if (!response.IsSuccess)
                {
                    if (response.Message.Contains("bulunamadı"))
                    {
                        return NotFound(new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = response.Message });
                    }
                    
                    if (response.Message.Contains("erişim izniniz yok"))
                    {
                        return Forbid();
                    }
                    
                    return BadRequest(new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = response.Message });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik istatistikleri getirilirken bir hata oluştu. EventId: {EventId}", eventId);
                return StatusCode(500, new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = "Etkinlik istatistikleri getirilirken bir hata oluştu" });
            }
        }
    }
} 