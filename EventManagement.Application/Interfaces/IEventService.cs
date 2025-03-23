using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    public interface IEventService
    {
        Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(Guid tenantId);
        Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id, Guid tenantId, bool isAdmin = false);
        Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync(Guid tenantId);
        Task<ResponseDto<List<EventDto>>> GetPendingEventsAsync(Guid tenantId);
        Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId);
        Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId);
        Task<ResponseDto<bool>> DeleteEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<bool>> CancelEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<bool>> ApproveEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<bool>> RejectEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id, Guid tenantId);
        
        // Dashboard istatistikleri için eklenen metodlar
        Task<int> GetTotalEventsCountAsync(Guid? tenantId);
        Task<int> GetActiveEventsCountAsync(Guid? tenantId);
        Task<int> GetUpcomingEventsCountAsync(Guid? tenantId);
    }
} 