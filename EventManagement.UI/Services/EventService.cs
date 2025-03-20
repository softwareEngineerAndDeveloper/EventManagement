using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Services
{
    public class EventService : IEventService
    {
        private readonly IApiService _apiService;

        public EventService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<EventDto>> GetAllEventsAsync(Guid tenantId)
        {
            var response = await _apiService.GetAllEventsAsync();
            return response.Data ?? new List<EventDto>();
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.GetEventByIdAsync(id);
            return response.Data;
        }

        public async Task<List<EventDto>> GetUpcomingEventsAsync(Guid tenantId)
        {
            var response = await _apiService.GetUpcomingEventsAsync();
            return response.Data ?? new List<EventDto>();
        }

        public async Task<List<EventDto>> GetPendingEventsAsync(Guid tenantId)
        {
            // PendingEvents API çağrısı eklenecek
            var response = await _apiService.GetAllEventsAsync(new EventFilterDto { Status = "Pending" });
            return response.Data?.Where(e => e.Status == EventStatus.Pending).ToList() ?? new List<EventDto>();
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId)
        {
            var response = await _apiService.CreateEventAsync(createEventDto);
            return response.Data ?? new EventDto();
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId)
        {
            var response = await _apiService.UpdateEventAsync(id, updateEventDto);
            return response.Data ?? new EventDto();
        }

        public async Task<bool> DeleteEventAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.DeleteEventAsync(id);
            return response.IsSuccess && response.Data;
        }

        public async Task<bool> ApproveEventAsync(Guid id, Guid tenantId)
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
                IsCancelled = eventDto.IsCancelled,
                Status = EventStatus.Approved
            };
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto);
            
            return response.IsSuccess && response.Data != null;
        }

        public async Task<bool> RejectEventAsync(Guid id, Guid tenantId)
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
                IsCancelled = eventDto.IsCancelled,
                Status = EventStatus.Rejected
            };
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto);
            
            return response.IsSuccess && response.Data != null;
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
            
            var response = await _apiService.UpdateEventAsync(id, updateEventDto);
            
            return response.IsSuccess && response.Data != null;
        }

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid id, Guid tenantId)
        {
            var response = await _apiService.GetEventStatisticsAsync(id);
            return response.Data ?? new EventStatisticsDto();
        }
    }
} 