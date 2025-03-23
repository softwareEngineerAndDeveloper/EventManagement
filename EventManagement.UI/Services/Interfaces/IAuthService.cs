using EventManagement.UI.Models;
using EventManagement.UI.Models.Auth;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EventManagement.UI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseModel>> LoginAsync(LoginViewModel model);
        Task<ApiResponse<bool>> RegisterAsync(RegisterViewModel model);
        Task<string?> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<bool> LogoutAsync();
        Task<bool> IsInRoleAsync(string role);
        Task<List<string>> GetUserRolesAsync();
        Task<Guid> GetCurrentUserIdAsync();
        Task<Guid> GetCurrentTenantIdAsync();
        
        Task<ResponseModel<string>> LoginAsync(string email, string password);
        Task<ResponseModel<bool>> RegisterAsync(UserRegistrationViewModel model);
        Task<ResponseModel<bool>> ChangePasswordAsync(ChangePasswordViewModel model, string token);
        Task<ResponseModel<EventManagement.UI.Models.User.UserViewModel>> GetCurrentUserAsync(string token);
    }
    
    public class UserRegistrationViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string PhoneNumber { get; set; }
        public Guid TenantId { get; set; }
    }
} 