using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    public interface IRegistrationService
    {
        Task<ResponseDto<List<RegistrationDto>>> GetAllRegistrationsAsync(Guid tenantId);
        Task<ResponseDto<RegistrationDto>> GetRegistrationByIdAsync(Guid id, Guid tenantId);
        Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByEventIdAsync(Guid eventId, Guid tenantId);
        Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByUserIdAsync(Guid userId, Guid tenantId);
        Task<ResponseDto<RegistrationDto>> CreateRegistrationAsync(CreateRegistrationDto createRegistrationDto, Guid tenantId, Guid currentUserId);
        Task<ResponseDto<RegistrationDto>> UpdateRegistrationAsync(Guid id, UpdateRegistrationDto updateRegistrationDto, Guid tenantId);
        Task<ResponseDto<bool>> DeleteRegistrationAsync(Guid id, Guid tenantId);
        Task<ResponseDto<bool>> CancelRegistrationAsync(Guid id, Guid tenantId, Guid currentUserId);
        Task<ResponseDto<bool>> MarkAttendanceAsync(Guid id, bool hasAttended, Guid tenantId);
        
        // Dashboard istatistikleri için eklenen metod
        Task<int> GetTotalRegistrationsCountAsync(Guid? tenantId);
    }
} 