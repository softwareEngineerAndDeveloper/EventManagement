using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;

namespace EventManagement.Application.Services
{
    /// <summary>
    /// Etkinlik katılımcıları yönetim servisi
    /// </summary>
    public class AttendeeService : IAttendeeService
    {
        private readonly IAttendeeRepository _attendeeRepository;
        private readonly ILogger<AttendeeService> _logger;

        public AttendeeService(IAttendeeRepository attendeeRepository, ILogger<AttendeeService> logger)
        {
            _attendeeRepository = attendeeRepository ?? throw new ArgumentNullException(nameof(attendeeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Etkinliğe ait tüm katılımcıları getirir
        /// </summary>
        public async Task<ResponseDto<List<AttendeeDto>>> GetAttendeesByEventIdAsync(Guid eventId, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Fetching attendees for event ID: {EventId}", eventId);
                
                // Önce etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(eventId);
                if (eventEntity == null)
                {
                    return ResponseDto<List<AttendeeDto>>.Fail("Etkinlik bulunamadı");
                }
                
                if (eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<List<AttendeeDto>>.Fail("Bu etkinliğe erişim izniniz yok");
                }
                
                var attendees = await _attendeeRepository.GetAttendeesByEventIdAsync(eventId);
                
                if (attendees == null || attendees.Count == 0)
                {
                    return ResponseDto<List<AttendeeDto>>.Success(new List<AttendeeDto>(), "Etkinliğe ait katılımcı bulunmamaktadır");
                }
                
                var attendeeDtos = attendees.Select(MapToAttendeeDto).ToList();
                
                return ResponseDto<List<AttendeeDto>>.Success(attendeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendees for event ID: {EventId}", eventId);
                return ResponseDto<List<AttendeeDto>>.Fail("Katılımcılar getirilirken bir hata oluştu");
            }
        }

        /// <summary>
        /// ID'ye göre katılımcı bilgisini getirir
        /// </summary>
        public async Task<ResponseDto<AttendeeDto>> GetAttendeeByIdAsync(Guid id, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Fetching attendee with ID: {Id}", id);
                
                var attendee = await _attendeeRepository.GetAttendeeByIdAsync(id);
                
                if (attendee == null)
                {
                    return ResponseDto<AttendeeDto>.Fail("Katılımcı bulunamadı");
                }
                
                // Katılımcının etkinliğini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(attendee.EventId);
                if (eventEntity == null || eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<AttendeeDto>.Fail("Bu katılımcıya erişim izniniz yok");
                }
                
                var attendeeDto = MapToAttendeeDto(attendee);
                
                return ResponseDto<AttendeeDto>.Success(attendeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendee with ID: {Id}", id);
                return ResponseDto<AttendeeDto>.Fail("Katılımcı bilgisi getirilirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Yeni katılımcı oluşturur
        /// </summary>
        public async Task<ResponseDto<AttendeeDto>> CreateAttendeeAsync(CreateAttendeeDto createAttendeeDto, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Creating new attendee for event ID: {EventId}", createAttendeeDto.EventId);
                
                // Önce etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(createAttendeeDto.EventId);
                if (eventEntity == null)
                {
                    return ResponseDto<AttendeeDto>.Fail("Etkinlik bulunamadı");
                }
                
                if (eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<AttendeeDto>.Fail("Bu etkinliğe katılımcı eklemek için izniniz yok");
                }
                
                // E-posta adresine göre mevcut katılımcı kontrolü
                var existingAttendee = await _attendeeRepository.SearchAttendeeByEmailAsync(createAttendeeDto.Email);
                if (existingAttendee != null && existingAttendee.EventId == createAttendeeDto.EventId && !existingAttendee.IsDeleted)
                {
                    return ResponseDto<AttendeeDto>.Fail("Bu e-posta adresi ile etkinliğe kayıtlı bir katılımcı zaten mevcut");
                }
                
                // Yeni katılımcı oluştur
                var attendee = new Attendee
                {
                    EventId = createAttendeeDto.EventId,
                    Name = createAttendeeDto.Name,
                    Email = createAttendeeDto.Email,
                    Phone = createAttendeeDto.Phone,
                    Status = await DetermineAttendeeStatusAsync(eventEntity),
                    RegistrationDate = DateTime.UtcNow,
                    Notes = createAttendeeDto.Notes,
                    HasAttended = false,
                    IsCancelled = false,
                    TenantId = tenantId
                };
                
                var createdAttendee = await _attendeeRepository.CreateAttendeeAsync(attendee);
                
                var attendeeDto = MapToAttendeeDto(createdAttendee);
                
                return ResponseDto<AttendeeDto>.Success(attendeeDto, "Katılımcı başarıyla oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendee for event ID: {EventId}", createAttendeeDto.EventId);
                return ResponseDto<AttendeeDto>.Fail("Katılımcı oluşturulurken bir hata oluştu");
            }
        }

        /// <summary>
        /// Mevcut katılımcı bilgisini günceller
        /// </summary>
        public async Task<ResponseDto<AttendeeDto>> UpdateAttendeeAsync(Guid id, UpdateAttendeeDto updateAttendeeDto, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Updating attendee with ID: {Id}", id);
                
                // Önce katılımcıyı getir
                var attendee = await _attendeeRepository.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    return ResponseDto<AttendeeDto>.Fail("Güncellenecek katılımcı bulunamadı");
                }
                
                // Etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(attendee.EventId);
                if (eventEntity == null || eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<AttendeeDto>.Fail("Bu katılımcıyı güncellemek için izniniz yok");
                }
                
                // Eğer e-posta değiştiyse, yeni e-postanın başka bir katılımcıda kullanılmadığından emin ol
                if (attendee.Email != updateAttendeeDto.Email)
                {
                    var existingAttendee = await _attendeeRepository.SearchAttendeeByEmailAsync(updateAttendeeDto.Email);
                    if (existingAttendee != null && existingAttendee.Id != id && existingAttendee.EventId == attendee.EventId && !existingAttendee.IsDeleted)
                    {
                        return ResponseDto<AttendeeDto>.Fail("Bu e-posta adresi ile etkinliğe kayıtlı başka bir katılımcı zaten mevcut");
                    }
                }
                
                // Değişiklikleri uygula
                attendee.Name = updateAttendeeDto.Name;
                attendee.Email = updateAttendeeDto.Email;
                attendee.Phone = updateAttendeeDto.Phone;
                attendee.Status = updateAttendeeDto.Status;
                attendee.HasAttended = updateAttendeeDto.HasAttended;
                attendee.Notes = updateAttendeeDto.Notes;
                
                var updatedAttendee = await _attendeeRepository.UpdateAttendeeAsync(attendee);
                
                var attendeeDto = MapToAttendeeDto(updatedAttendee);
                
                return ResponseDto<AttendeeDto>.Success(attendeeDto, "Katılımcı bilgileri başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendee with ID: {Id}", id);
                return ResponseDto<AttendeeDto>.Fail("Katılımcı güncellenirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Katılımcıyı soft-delete yapar
        /// </summary>
        public async Task<ResponseDto<bool>> DeleteAttendeeAsync(Guid id, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Deleting attendee with ID: {Id}", id);
                
                // Önce katılımcıyı getir
                var attendee = await _attendeeRepository.GetAttendeeByIdAsync(id);
                if (attendee == null)
                {
                    return ResponseDto<bool>.Fail("Silinecek katılımcı bulunamadı");
                }
                
                // Etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(attendee.EventId);
                if (eventEntity == null || eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<bool>.Fail("Bu katılımcıyı silmek için izniniz yok");
                }
                
                var result = await _attendeeRepository.DeleteAttendeeAsync(id);
                
                if (result)
                {
                    return ResponseDto<bool>.Success(true, "Katılımcı başarıyla silindi");
                }
                
                return ResponseDto<bool>.Fail("Katılımcı silinemedi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendee with ID: {Id}", id);
                return ResponseDto<bool>.Fail("Katılımcı silinirken bir hata oluştu");
            }
        }

        /// <summary>
        /// E-posta adresine göre katılımcı arar
        /// </summary>
        public async Task<ResponseDto<AttendeeDto>> SearchAttendeeByEmailAsync(string email, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Searching attendee by email: {Email}", email);
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ResponseDto<AttendeeDto>.Fail("E-posta adresi belirtilmelidir");
                }
                
                var attendee = await _attendeeRepository.SearchAttendeeByEmailAsync(email);
                
                if (attendee == null)
                {
                    return ResponseDto<AttendeeDto>.Fail("Katılımcı bulunamadı");
                }
                
                // Etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(attendee.EventId);
                if (eventEntity == null || eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<AttendeeDto>.Fail("Bu katılımcıya erişim izniniz yok");
                }
                
                var attendeeDto = MapToAttendeeDto(attendee);
                
                return ResponseDto<AttendeeDto>.Success(attendeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching attendee by email: {Email}", email);
                return ResponseDto<AttendeeDto>.Fail("Katılımcı aranırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Belirtilen kriterlere göre katılımcı arar
        /// </summary>
        public async Task<ResponseDto<List<AttendeeDto>>> SearchAttendeesAsync(SearchAttendeeDto searchCriteria, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Searching attendees with criteria");
                
                if (searchCriteria.EventId == null)
                {
                    return ResponseDto<List<AttendeeDto>>.Fail("Etkinlik ID belirtilmelidir");
                }
                
                // Etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(searchCriteria.EventId.Value);
                if (eventEntity == null)
                {
                    return ResponseDto<List<AttendeeDto>>.Fail("Etkinlik bulunamadı");
                }
                
                if (eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<List<AttendeeDto>>.Fail("Bu etkinliğin katılımcılarını aramak için izniniz yok");
                }
                
                // Etkinliğe ait tüm katılımcıları getir ve filtrele
                var attendees = await _attendeeRepository.GetAttendeesByEventIdAsync(searchCriteria.EventId.Value);
                
                // İsim veya e-posta adresine göre filtrele
                var filteredAttendees = attendees;
                
                if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
                {
                    filteredAttendees = filteredAttendees.Where(a => a.Name.Contains(searchCriteria.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(searchCriteria.Email))
                {
                    filteredAttendees = filteredAttendees.Where(a => a.Email.Contains(searchCriteria.Email, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                var attendeeDtos = filteredAttendees.Select(MapToAttendeeDto).ToList();
                
                return ResponseDto<List<AttendeeDto>>.Success(attendeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching attendees");
                return ResponseDto<List<AttendeeDto>>.Fail("Katılımcılar aranırken bir hata oluştu");
            }
        }

        /// <summary>
        /// Etkinlik istatistiklerini getirir
        /// </summary>
        public async Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid eventId, Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Getting event statistics for event ID: {EventId}", eventId);
                
                // Önce etkinlik bilgisini getir ve tenant kontrolü yap
                var eventEntity = await _attendeeRepository.GetEventByIdAsync(eventId);
                if (eventEntity == null)
                {
                    return ResponseDto<EventStatisticsDto>.Fail("Etkinlik bulunamadı");
                }
                
                if (eventEntity.TenantId != tenantId)
                {
                    return ResponseDto<EventStatisticsDto>.Fail("Bu etkinliğin istatistiklerini görüntülemek için izniniz yok");
                }
                
                // Repository üzerinden etkinlik istatistiklerini al
                var statistics = await _attendeeRepository.GetEventStatisticsAsync(eventId);
                
                return ResponseDto<EventStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event statistics for event ID: {EventId}", eventId);
                return ResponseDto<EventStatisticsDto>.Fail("Etkinlik istatistikleri getirilirken bir hata oluştu");
            }
        }

        /// <summary>
        /// Attendee nesnesini AttendeeDto'ya dönüştürür
        /// </summary>
        private AttendeeDto MapToAttendeeDto(Attendee attendee)
        {
            return new AttendeeDto
            {
                Id = attendee.Id,
                EventId = attendee.EventId,
                Name = attendee.Name,
                Email = attendee.Email,
                Phone = attendee.Phone,
                Status = attendee.Status,
                HasAttended = attendee.HasAttended,
                RegistrationDate = attendee.RegistrationDate,
                Notes = attendee.Notes,
                IsCancelled = attendee.IsCancelled
            };
        }

        /// <summary>
        /// Yeni eklenen katılımcının durumunu belirler
        /// </summary>
        private async Task<int> DetermineAttendeeStatusAsync(Event eventEntity)
        {
            // Etkinliğin maksimum katılımcı sayısını ve mevcut onaylanmış katılımcı sayısını kontrol et
            if (eventEntity.MaxAttendees.HasValue)
            {
                int confirmedCount = await _attendeeRepository.GetConfirmedAttendeesCountAsync(eventEntity.Id);
                
                // Eğer etkinlik doluysa, yeni katılımcıyı bekleme listesine ekle
                if (confirmedCount >= eventEntity.MaxAttendees.Value)
                {
                    return 0; // Bekleme listesinde
                }
            }
            
            return 1; // Onaylandı
        }
    }
} 