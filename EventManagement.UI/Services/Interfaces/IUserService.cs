using EventManagement.UI.Models;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.User;

namespace EventManagement.UI.Services.Interfaces
{
    public interface IUserService
    {
        Task<ResponseModel<List<UserViewModel>>> GetAllUsersAsync(string token);
        Task<ResponseModel<UserViewModel>> GetUserByIdAsync(Guid id, string token);
        Task<ResponseModel<UserViewModel>> GetCurrentUserAsync(string token);
        Task<ResponseModel<UserViewModel>> UpdateUserAsync(UpdateUserViewModel model, string token);
        Task<ResponseModel<bool>> DeleteUserAsync(Guid id, string token);
    }
} 