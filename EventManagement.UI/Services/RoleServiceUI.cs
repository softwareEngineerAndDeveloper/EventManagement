using EventManagement.UI.Models;
using EventManagement.UI.Interfaces;

namespace EventManagement.UI.Services
{
    public class RoleServiceUI : IRoleServiceUI
    {
        private readonly IApiServiceUI _apiService;

        public RoleServiceUI(IApiServiceUI apiService)
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