using EventManagement.UI.DTOs;
using EventManagement.UI.Interfaces;

namespace EventManagement.UI.Services
{
    public class AttendeeServiceUI : IAttendeeServiceUI
    {
        private readonly IApiServiceUI _apiService;
        private readonly ILogger<AttendeeServiceUI> _logger;

        public AttendeeServiceUI(IApiServiceUI apiService, ILogger<AttendeeServiceUI> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<List<AttendeeDto>> GetAttendeesByEventIdAsync(Guid eventId)
        {
            try
            {
                var response = await _apiService.GetAttendeesByEventIdAsync(eventId);
                return response.Data ?? new List<AttendeeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcılar getirilirken hata oluştu. EventId: {EventId}", eventId);
                return new List<AttendeeDto>();
            }
        }

        public async Task<AttendeeDto?> GetAttendeeByIdAsync(Guid id)
        {
            try
            {
                var response = await _apiService.GetAttendeeByIdAsync(id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı bilgisi getirilirken hata oluştu. Id: {Id}", id);
                return null;
            }
        }

        public async Task<AttendeeDto?> CreateAttendeeAsync(AttendeeDto attendeeDto)
        {
            try
            {
                var createDto = new CreateAttendeeDto
                {
                    EventId = attendeeDto.EventId,
                    Name = attendeeDto.Name,
                    Email = attendeeDto.Email,
                    Phone = attendeeDto.Phone,
                    Notes = attendeeDto.Notes
                };
                
                _logger.LogInformation("Katılımcı oluşturuluyor. EventId: {EventId}, Email: {Email}", 
                                      attendeeDto.EventId, attendeeDto.Email);
                
                var response = await _apiService.CreateAttendeeAsync(createDto);
                
                if (response.IsSuccess && response.Data != null)
                {
                    _logger.LogInformation("Katılımcı başarıyla oluşturuldu. Id: {Id}", response.Data.Id);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("Katılımcı oluşturulamadı. Hata: {Message}", response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı oluşturulurken hata oluştu. EventId: {EventId}", attendeeDto.EventId);
                return null;
            }
        }

        public async Task<AttendeeDto?> UpdateAttendeeAsync(AttendeeDto attendeeDto)
        {
            try
            {
                var updateDto = new UpdateAttendeeDto
                {
                    Name = attendeeDto.Name,
                    Email = attendeeDto.Email,
                    Phone = attendeeDto.Phone,
                    Status = attendeeDto.Status,
                    HasAttended = attendeeDto.HasAttended,
                    Notes = attendeeDto.Notes
                };
                
                var response = await _apiService.UpdateAttendeeAsync(attendeeDto.Id, updateDto);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı güncellenirken hata oluştu. Id: {Id}", attendeeDto.Id);
                return null;
            }
        }

        public async Task<bool> DeleteAttendeeAsync(Guid id)
        {
            try
            {
                var response = await _apiService.DeleteAttendeeAsync(id);
                return response.IsSuccess && response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcı silinirken hata oluştu. Id: {Id}", id);
                return false;
            }
        }

        public async Task<List<AttendeeDto>> SearchAttendeesAsync(Guid eventId, string? searchTerm = null, int? status = null)
        {
            try
            {
                var searchDto = new SearchAttendeeDto
                {
                    EventId = eventId,
                    Name = searchTerm,
                    Email = searchTerm,
                    Status = status
                };
                
                var response = await _apiService.SearchAttendeesAsync(searchDto);
                return response.Data ?? new List<AttendeeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Katılımcılar aranırken hata oluştu. EventId: {EventId}, SearchTerm: {SearchTerm}, Status: {Status}", 
                    eventId, searchTerm, status);
                return new List<AttendeeDto>();
            }
        }

        public async Task<AttendeeDto?> SearchAttendeeByEmailAsync(string email)
        {
            try
            {
                var response = await _apiService.SearchAttendeeByEmailAsync(email);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta ile katılımcı aranırken hata oluştu. Email: {Email}", email);
                return null;
            }
        }

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId)
        {
            try
            {
                // test amaçlı eklenmiş tenant id sessiondan alınacak
                Guid tenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                var response = await _apiService.GetEventStatisticsAsync(eventId, tenantId);
                return response.Data ?? new EventStatisticsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik istatistikleri getirilirken hata oluştu. EventId: {EventId}", eventId);
                return new EventStatisticsDto { EventId = eventId };
            }
        }
    }
} 