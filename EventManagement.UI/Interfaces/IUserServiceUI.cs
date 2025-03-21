using EventManagement.UI.DTOs;
using EventManagement.UI.Models;

namespace EventManagement.UI.Interfaces
{
    public interface IUserServiceUI
    {
        Task<List<UserViewModel>> GetAllUsersAsync(Guid tenantId);
        Task<UserViewModel?> GetUserByIdAsync(Guid id, Guid tenantId);
        Task<UserViewModel?> GetUserByEmailAsync(string email, Guid tenantId);
        Task<UserViewModel> CreateUserAsync(RegisterDto registerDto, Guid tenantId);
        Task<UserViewModel> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId);
        Task<bool> DeleteUserAsync(Guid id, Guid tenantId);
        Task<bool> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId);
        Task<bool> ActivateUserAsync(Guid id, Guid tenantId);
        Task<bool> DeactivateUserAsync(Guid id, Guid tenantId);
    }
} 