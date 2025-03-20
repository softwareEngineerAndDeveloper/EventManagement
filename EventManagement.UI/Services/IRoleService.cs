using EventManagement.UI.Models;

namespace EventManagement.UI.Services
{
    public interface IRoleService
    {
        Task<List<RoleViewModel>> GetAllRolesAsync(Guid tenantId);
        Task<RoleViewModel?> GetRoleByIdAsync(Guid id, Guid tenantId);
    }
} 