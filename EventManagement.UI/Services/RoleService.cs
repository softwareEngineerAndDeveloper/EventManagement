using EventManagement.UI.Models;

namespace EventManagement.UI.Services
{
    public class RoleService : IRoleService
    {
        private readonly IApiService _apiService;

        public RoleService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<RoleViewModel>> GetAllRolesAsync(Guid tenantId)
        {
            // API'da bu metod eklenmeli
            var roles = new List<RoleViewModel>();
            
            roles.Add(new RoleViewModel 
            { 
                Id = Guid.NewGuid(), 
                Name = "Admin", 
                Description = "Sistem Yöneticisi" 
            });
            
            roles.Add(new RoleViewModel 
            { 
                Id = Guid.NewGuid(), 
                Name = "EventManager", 
                Description = "Etkinlik Yöneticisi" 
            });
            
            roles.Add(new RoleViewModel 
            { 
                Id = Guid.NewGuid(), 
                Name = "Attendee", 
                Description = "Etkinlik Katılımcısı" 
            });
            
            return roles;
        }

        public async Task<RoleViewModel?> GetRoleByIdAsync(Guid id, Guid tenantId)
        {
            // API'da bu metod eklenmeli
            return new RoleViewModel
            {
                Id = id,
                Name = "Admin",
                Description = "Sistem Yöneticisi"
            };
        }
    }
} 