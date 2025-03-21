using EventManagement.UI.Models;

namespace EventManagement.UI.Interfaces
{
    public interface IRoleServiceUI
    {
        Task<List<RoleViewModel>> GetAllRolesAsync(Guid tenantId);
        Task<RoleViewModel?> GetRoleByIdAsync(Guid id, Guid tenantId);
    }
}