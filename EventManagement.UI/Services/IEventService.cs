using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Services
{
    public interface IEventService
    {
        Task<List<EventDto>> GetAllEventsAsync(Guid tenantId);
        Task<EventDto?> GetEventByIdAsync(Guid id, Guid tenantId);
        Task<List<EventDto>> GetUpcomingEventsAsync(Guid tenantId);
        Task<List<EventDto>> GetPendingEventsAsync(Guid tenantId);
        Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId);
        Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId);
        Task<bool> DeleteEventAsync(Guid id, Guid tenantId);
        Task<bool> ApproveEventAsync(Guid id, Guid tenantId);
        Task<bool> RejectEventAsync(Guid id, Guid tenantId);
        Task<bool> CancelEventAsync(Guid id, Guid tenantId);
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid id, Guid tenantId);
    }
} 