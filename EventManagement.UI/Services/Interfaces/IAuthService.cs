using EventManagement.UI.Models.Auth;
using EventManagement.UI.Models.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EventManagement.UI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseModel>> LoginAsync(LoginViewModel model);
        Task<ApiResponse<bool>> RegisterAsync(RegisterViewModel model);
        string? GetTokenAsync();
        bool IsAuthenticatedAsync();
        Task<bool> LogoutAsync();
        bool IsInRoleAsync(string role);
        List<string> GetUserRolesAsync();
        Guid GetCurrentUserIdAsync();
        Guid GetCurrentTenantIdAsync();
    }
} 