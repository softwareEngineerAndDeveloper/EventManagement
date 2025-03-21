using EventManagement.UI.DTOs;

namespace EventManagement.UI.Interfaces
{
    public interface IApiServiceUI
    {
        // Auth işlemleri
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetCurrentUserAsync();
        Task<bool> UpdateCurrentUserAsync(UpdateProfileDto updateProfileDto);
        Task<bool> LogoutAsync();
        Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto);

        // Event işlemleri
        Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(Guid tenantId);
        Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(EventFilterDto? filter = null, Guid? tenantId = null);
        Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id, Guid tenantId);
        Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId);
        Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId);
        Task<ResponseDto<bool>> DeleteEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id, Guid tenantId);
        Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync(Guid tenantId);
        Task<ResponseDto<bool>> ApproveEventAsync(Guid id, Guid tenantId);
        Task<ResponseDto<bool>> RejectEventAsync(Guid id, Guid tenantId);

        // Attendee işlemleri
        Task<ResponseDto<List<AttendeeDto>>> GetAttendeesByEventIdAsync(Guid eventId);
        Task<ResponseDto<AttendeeDto>> GetAttendeeByIdAsync(Guid id);
        Task<ResponseDto<AttendeeDto>> CreateAttendeeAsync(CreateAttendeeDto createAttendeeDto);
        Task<ResponseDto<AttendeeDto>> UpdateAttendeeAsync(Guid id, UpdateAttendeeDto updateAttendeeDto);
        Task<ResponseDto<bool>> DeleteAttendeeAsync(Guid id);
        Task<ResponseDto<AttendeeDto>> SearchAttendeeByEmailAsync(string email);
        Task<ResponseDto<List<AttendeeDto>>> SearchAttendeesAsync(SearchAttendeeDto searchDto);

        // Registration işlemleri
        Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByEventIdAsync(Guid eventId);
        Task<ResponseDto<RegistrationDto>> GetRegistrationByIdAsync(Guid id);
        Task<ResponseDto<RegistrationDto>> CreateRegistrationAsync(Guid eventId, CreateRegistrationDto createRegistrationDto);
        Task<ResponseDto<RegistrationDto>> UpdateRegistrationAsync(Guid eventId, Guid id, UpdateRegistrationDto updateRegistrationDto);
        Task<ResponseDto<bool>> DeleteRegistrationAsync(Guid eventId, Guid id);

        // Tenant işlemleri
        Task<ResponseDto<TenantDto>> GetCurrentTenantAsync();
        Task<ResponseDto<TenantDto>> GetTenantByIdAsync(Guid id);
        Task<ResponseDto<TenantDto>> GetTenantBySubdomainAsync(string subdomain);
        Task<ResponseDto<List<TenantDto>>> GetAllTenantsAsync();
        Task<ResponseDto<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto);
        Task<ResponseDto<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantDto updateTenantDto);

        // User işlemleri
        Task<ResponseDto<List<UserDto>>> GetAllUsersAsync();
        Task<ResponseDto<UserDto>> GetUserByIdAsync(Guid id);
        Task<ResponseDto<UserDto>> CreateUserAsync(RegisterDto createUserDto);
        Task<ResponseDto<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<ResponseDto<bool>> DeleteUserAsync(Guid id);

        // Role işlemleri
        Task<ResponseDto<List<RoleDto>>> GetAllRolesAsync();
        Task<ResponseDto<RoleDto>> GetRoleByIdAsync(Guid id);
        Task<ResponseDto<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<ResponseDto<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto);
        Task<ResponseDto<bool>> DeleteRoleAsync(Guid id);
    }
} 