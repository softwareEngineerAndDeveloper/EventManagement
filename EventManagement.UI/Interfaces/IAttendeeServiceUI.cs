using EventManagement.UI.DTOs;

namespace EventManagement.UI.Interfaces
{
    public interface IAttendeeServiceUI
    {
        Task<List<AttendeeDto>> GetAttendeesByEventIdAsync(Guid eventId);
        Task<AttendeeDto?> GetAttendeeByIdAsync(Guid id);
        Task<AttendeeDto?> CreateAttendeeAsync(AttendeeDto attendeeDto);
        Task<AttendeeDto?> UpdateAttendeeAsync(AttendeeDto attendeeDto);
        Task<bool> DeleteAttendeeAsync(Guid id);
        Task<List<AttendeeDto>> SearchAttendeesAsync(Guid eventId, string? searchTerm = null, int? status = null);
        Task<AttendeeDto?> SearchAttendeeByEmailAsync(string email);
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId);
    }
} 