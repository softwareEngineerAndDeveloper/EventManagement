using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Services
{
    public interface IApiService
    {
        // Auth işlemleri
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetCurrentUserAsync();

        // Event işlemleri
        Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(EventFilterDto? filter = null);
        Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id);
        Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto);
        Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto);
        Task<ResponseDto<bool>> DeleteEventAsync(Guid id);
        Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id);
        Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync();

        // Registration işlemleri
        Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByEventIdAsync(Guid eventId);
        Task<ResponseDto<RegistrationDto>> GetRegistrationByIdAsync(Guid id);
        Task<ResponseDto<RegistrationDto>> CreateRegistrationAsync(Guid eventId, CreateRegistrationDto createRegistrationDto);
        Task<ResponseDto<RegistrationDto>> UpdateRegistrationAsync(Guid eventId, Guid id, UpdateRegistrationDto updateRegistrationDto);
        Task<ResponseDto<bool>> DeleteRegistrationAsync(Guid eventId, Guid id);

        // Tenant işlemleri
        Task<ResponseDto<TenantDto>> GetCurrentTenantAsync();
        Task<ResponseDto<TenantDto>> GetTenantByIdAsync(Guid id);
        Task<ResponseDto<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto);
        Task<ResponseDto<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantDto updateTenantDto);
    }
} 