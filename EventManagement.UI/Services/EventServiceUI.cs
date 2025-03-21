using EventManagement.Domain.Entities;
using EventManagement.UI.DTOs;
using EventManagement.UI.Interfaces;

namespace EventManagement.UI.Services
{
    public class EventServiceUI : IEventServiceUI
    {
        private readonly IApiServiceUI _apiService;

        public EventServiceUI(IApiServiceUI apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<EventDto>> GetAllEventsAsync(Guid tenantId)
        {
            var response = await _apiService.GetAllEventsAsync(tenantId);
            return response.Data ?? new List<EventDto>();
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.GetEventByIdAsync(id, tenantId);
            return response.Data;
        }

        public async Task<List<EventDto>> GetUpcomingEventsAsync(Guid tenantId)
        {
            var response = await _apiService.GetUpcomingEventsAsync(tenantId);
            return response.Data ?? new List<EventDto>();
        }

        public async Task<List<EventDto>> GetPendingEventsAsync(Guid tenantId)
        {
            // PendingEvents API çağrısı eklenecek
            var response = await _apiService.GetAllEventsAsync(new EventFilterDto { Status = "Pending" }, tenantId);
            return response.Data?.Where(e => e.Status == EventStatus.Pending).ToList() ?? new List<EventDto>();
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId)
        {
            var response = await _apiService.CreateEventAsync(createEventDto, tenantId);
            return response.Data ?? new EventDto();
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId)
        {
            var response = await _apiService.UpdateEventAsync(id, updateEventDto, tenantId);
            return response.Data ?? new EventDto();
        }

        public async Task<bool> DeleteEventAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.DeleteEventAsync(id, tenantId);
            return response.IsSuccess && response.Data;
        }

        public async Task<bool> ApproveEventAsync(Guid id, Guid tenantId)
        {
            var eventDto = await GetEventByIdAsync(id, tenantId);
            
            if (eventDto == null)
            {
                Console.WriteLine($"ApproveEvent: Event bulunamadı. ID: {id}, TenantId: {tenantId}");
                return false;
            }
            
            Console.WriteLine($"ApproveEvent: Etkinlik durumu (Öncesi) - ID: {id}, Status: {eventDto.Status}");
            
            var updateEventDto = new UpdateEventDto
            {
                Id = eventDto.Id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartDate = eventDto.StartDate,
                EndDate = eventDto.EndDate,
                Location = eventDto.Location,
                Capacity = eventDto.Capacity,
                IsActive = true, // Etkinliği onaylarken aktif yapıyoruz
                MaxAttendees = eventDto.MaxAttendees,
                IsPublic = eventDto.IsPublic,
                IsCancelled = false, // İptal edilmiş olabilecek etkinlikler için
                Status = EventStatus.Approved // Onaylandı olarak işaretliyoruz (1)
            };
            
            Console.WriteLine($"ApproveEvent: Etkinlik güncelleniyor - Status: {updateEventDto.Status}, IsActive: {updateEventDto.IsActive}");
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto, tenantId);
            
            if (response.IsSuccess && response.Data != null)
            {
                Console.WriteLine($"ApproveEvent: Etkinlik onaylandı - ID: {id}, Yeni Status: {response.Data.Status}");
                return true;
            }
            else
            {
                Console.WriteLine($"ApproveEvent: Etkinlik onaylanamadı - ID: {id}, Hata: {response.Message}");
                return false;
            }
        }

        public async Task<bool> RejectEventAsync(Guid id, Guid tenantId)
        {
            var eventDto = await GetEventByIdAsync(id, tenantId);
            
            if (eventDto == null)
            {
                Console.WriteLine($"RejectEvent: Event bulunamadı. ID: {id}, TenantId: {tenantId}");
                return false;
            }
            
            Console.WriteLine($"RejectEvent: Etkinlik durumu (Öncesi) - ID: {id}, Status: {eventDto.Status}");
            
            var updateEventDto = new UpdateEventDto
            {
                Id = eventDto.Id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartDate = eventDto.StartDate,
                EndDate = eventDto.EndDate,
                Location = eventDto.Location,
                Capacity = eventDto.Capacity,
                IsActive = false, // Etkinliği reddederken pasif yapıyoruz
                MaxAttendees = eventDto.MaxAttendees,
                IsPublic = eventDto.IsPublic,
                IsCancelled = false,
                Status = EventStatus.Rejected // Reddedildi olarak işaretliyoruz (2)
            };
            
            Console.WriteLine($"RejectEvent: Etkinlik güncelleniyor - Status: {updateEventDto.Status}, IsActive: {updateEventDto.IsActive}");
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto, tenantId);
            
            if (response.IsSuccess && response.Data != null)
            {
                Console.WriteLine($"RejectEvent: Etkinlik reddedildi - ID: {id}, Yeni Status: {response.Data.Status}");
                return true;
            }
            else
            {
                Console.WriteLine($"RejectEvent: Etkinlik reddedilemedi - ID: {id}, Hata: {response.Message}");
                return false;
            }
        }

        public async Task<bool> CancelEventAsync(Guid id, Guid tenantId)
        {
            var eventDto = await GetEventByIdAsync(id, tenantId);
            
            if (eventDto == null)
            {
                return false;
            }
            
            var updateEventDto = new UpdateEventDto
            {
                Id = eventDto.Id,
                Title = eventDto.Title,
                Description = eventDto.Description,
                StartDate = eventDto.StartDate,
                EndDate = eventDto.EndDate,
                Location = eventDto.Location,
                Capacity = eventDto.Capacity,
                IsActive = eventDto.IsActive,
                MaxAttendees = eventDto.MaxAttendees,
                IsPublic = eventDto.IsPublic,
                IsCancelled = true,
                Status = EventStatus.Cancelled
            };
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto, tenantId);
            
            return response.IsSuccess && response.Data != null;
        }

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.GetEventStatisticsAsync(id, tenantId);
            return response.Data ?? new EventStatisticsDto();
        }
    }
} 