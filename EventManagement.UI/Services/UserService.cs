using EventManagement.UI.Models;
using EventManagement.UI.Models.DTOs;

namespace EventManagement.UI.Services
{
    public class UserService : IUserService
    {
        private readonly IApiService _apiService;

        public UserService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<UserViewModel>> GetAllUsersAsync(Guid tenantId)
        {
            // API'da bu metod eklenmeli
            var users = new List<UserViewModel>();
            
            // Şimdilik test için bir kullanıcı ekleyelim
            users.Add(new UserViewModel
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@etkinlikyonetimi.com",
                PhoneNumber = "5551234567",
                IsActive = true,
                CreatedDate = DateTime.Now,
                Roles = new List<RoleViewModel>
                {
                    new RoleViewModel { Id = Guid.NewGuid(), Name = "Admin", Description = "Sistem Yöneticisi" }
                }
            });
            
            return users;
        }

        public async Task<UserViewModel?> GetUserByIdAsync(Guid id, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return new UserViewModel
            {
                Id = id,
                FirstName = "Test",
                LastName = "User",
                Email = "test@etkinlikyonetimi.com",
                PhoneNumber = "5551234567",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<UserViewModel?> GetUserByEmailAsync(string email, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return new UserViewModel
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PhoneNumber = "5551234567",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<UserViewModel> CreateUserAsync(RegisterDto registerDto, Guid tenantId)
        {
            // API'ya RegisterDto gönderilip UserViewModel dönülmeli
            var response = await _apiService.RegisterAsync(registerDto);
            return new UserViewModel
            {
                Id = Guid.NewGuid(),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<UserViewModel> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return new UserViewModel
            {
                Id = id,
                FirstName = updateUserDto.FirstName,
                LastName = updateUserDto.LastName,
                PhoneNumber = updateUserDto.PhoneNumber,
                Email = "test@example.com",
                IsActive = updateUserDto.IsActive,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<bool> DeleteUserAsync(Guid id, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return true;
        }

        public async Task<bool> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return true;
        }
    }
} 