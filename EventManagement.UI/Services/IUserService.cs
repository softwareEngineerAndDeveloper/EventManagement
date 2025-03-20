using EventManagement.UI.Models;
using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Services
{
    public interface IUserService
    {
        Task<List<UserViewModel>> GetAllUsersAsync(Guid tenantId);
        Task<UserViewModel?> GetUserByIdAsync(Guid id, Guid tenantId);
        Task<UserViewModel?> GetUserByEmailAsync(string email, Guid tenantId);
        Task<UserViewModel> CreateUserAsync(RegisterDto registerDto, Guid tenantId);
        Task<UserViewModel> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId);
        Task<bool> DeleteUserAsync(Guid id, Guid tenantId);
        Task<bool> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId);
    }
} 