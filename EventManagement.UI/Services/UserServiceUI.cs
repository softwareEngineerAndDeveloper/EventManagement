using EventManagement.UI.Models;
using EventManagement.UI.Interfaces;
using EventManagement.UI.DTOs;

namespace EventManagement.UI.Services
{
    public class UserServiceUI : IUserServiceUI
    {
        private readonly IApiServiceUI _apiService;

        // Statik kullanıcı listesi oluşturuyoruz
        private static List<UserViewModel> _users = new List<UserViewModel>();

        public UserServiceUI(IApiServiceUI apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<UserViewModel>> GetAllUsersAsync(Guid tenantId)
        {
            // Eğer liste boşsa, örnek kullanıcıları ekle
            if (_users.Count == 0)
            {
                // Admin kullanıcısı
                _users.Add(new UserViewModel
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
                
                // Event Manager kullanıcısı
                _users.Add(new UserViewModel
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Etkinlik",
                    LastName = "Yöneticisi",
                    Email = "manager@etkinlikyonetimi.com",
                    PhoneNumber = "5552345678",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    Roles = new List<RoleViewModel>
                    {
                        new RoleViewModel { Id = Guid.NewGuid(), Name = "EventManager", Description = "Etkinlik Yöneticisi" }
                    }
                });
                
                // Normal kullanıcı
                _users.Add(new UserViewModel
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Test",
                    LastName = "Kullanıcı",
                    Email = "user@etkinlikyonetimi.com",
                    PhoneNumber = "5553456789",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    Roles = new List<RoleViewModel>
                    {
                        new RoleViewModel { Id = Guid.NewGuid(), Name = "Attendee", Description = "Etkinlik Katılımcısı" }
                    }
                });
            }
            
            return _users;
        }

        public async Task<UserViewModel?> GetUserByIdAsync(Guid id, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public async Task<UserViewModel?> GetUserByEmailAsync(string email, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return _users.FirstOrDefault(u => u.Email == email);
        }

        public async Task<UserViewModel> CreateUserAsync(RegisterDto registerDto, Guid tenantId)
        {
            // Yeni kullanıcı oluştur
            var newUser = new UserViewModel
            {
                Id = Guid.NewGuid(),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true,
                CreatedDate = DateTime.Now,
                Roles = new List<RoleViewModel>
                {
                    new RoleViewModel { Id = Guid.NewGuid(), Name = "User", Description = "Kullanıcı" }
                }
            };
            
            // Statik listeye ekle
            _users.Add(newUser);
            
            // API'ya RegisterDto gönderilip UserViewModel dönülmeli
            var response = await _apiService.RegisterAsync(registerDto);
            return newUser;
        }

        public async Task<UserViewModel> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            var user = await GetUserByIdAsync(id, tenantId);
            if (user == null)
            {
                throw new ArgumentException("Kullanıcı bulunamadı");
            }

            user.FirstName = updateUserDto.FirstName;
            user.LastName = updateUserDto.LastName;
            user.PhoneNumber = updateUserDto.PhoneNumber;
            user.IsActive = updateUserDto.IsActive;
            user.CreatedDate = DateTime.Now;

            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid id, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            var user = await GetUserByIdAsync(id, tenantId);
            if (user == null)
            {
                return false;
            }

            _users.Remove(user);
            return true;
        }

        public async Task<bool> AssignRoleToUserAsync(AssignRoleDto assignRoleDto, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            var user = await GetUserByIdAsync(assignRoleDto.UserId, tenantId);
            if (user == null)
            {
                return false;
            }

            user.Roles = new List<RoleViewModel>
            {
                new RoleViewModel { Id = Guid.NewGuid(), Name = assignRoleDto.RoleName, Description = assignRoleDto.RoleDescription }
            };

            await UpdateUserAsync(assignRoleDto.UserId, new UpdateUserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive
            }, tenantId);

            return true;
        }

        public async Task<bool> ActivateUserAsync(Guid id, Guid tenantId)
        {
            var user = await GetUserByIdAsync(id, tenantId);
            if (user == null)
            {
                return false;
            }

            var updateUserDto = new UpdateUserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = true
            };

            await UpdateUserAsync(id, updateUserDto, tenantId);
            return true;
        }

        public async Task<bool> DeactivateUserAsync(Guid id, Guid tenantId)
        {
            var user = await GetUserByIdAsync(id, tenantId);
            if (user == null)
            {
                return false;
            }

            var updateUserDto = new UpdateUserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = false
            };

            await UpdateUserAsync(id, updateUserDto, tenantId);
            return true;
        }
    }
} 