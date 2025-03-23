using EventManagement.UI.Models;
using EventManagement.UI.Models.Role;
using EventManagement.UI.Models.Shared;

namespace EventManagement.UI.Services.Interfaces
{
    public interface IRoleService
    {
        Task<ResponseModel<List<RoleViewModel>>> GetAllRolesAsync(string token);
        Task<ResponseModel<RoleViewModel>> GetRoleByIdAsync(Guid id, string token);
        Task<ResponseModel<RoleViewModel>> CreateRoleAsync(CreateRoleViewModel model, string token);
        Task<ResponseModel<RoleViewModel>> UpdateRoleAsync(UpdateRoleViewModel model, string token);
        Task<ResponseModel<bool>> DeleteRoleAsync(Guid id, string token);
    }
} 