using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<ResponseDto<List<UserDto>>> GetAllUsersAsync(Guid tenantId);
        Task<ResponseDto<UserDto>> GetUserByIdAsync(Guid id, Guid tenantId, bool isAdmin = false);
        Task<ResponseDto<UserDto>> GetUserByEmailAsync(string email, Guid tenantId);
        Task<ResponseDto<UserDto>> CreateUserAsync(CreateUserDto createUserDto, Guid tenantId);
        Task<ResponseDto<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId);
        Task<ResponseDto<bool>> DeleteUserAsync(Guid id, Guid tenantId, bool isAdmin = false);
        Task<ResponseDto<string>> AuthenticateAsync(LoginDto loginDto);
        Task<ResponseDto<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, Guid tenantId);
        Task<ResponseDto<bool>> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId);
        Task<ResponseDto<List<RoleDto>>> GetRolesAsync(Guid tenantId);
        Task<ResponseDto<List<Guid>>> GetUserRolesAsync(Guid userId, Guid tenantId);
        
        // Dashboard istatistikleri için eklenen metodlar
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetTotalUsersByTenantAsync(Guid tenantId);
    }
} 