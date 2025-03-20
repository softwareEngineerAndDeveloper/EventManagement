using EventManagement.UI.Models;
using EventManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.UI.ViewComponents
{
    public class RolesViewComponent : ViewComponent
    {
        private readonly IRoleService _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RolesViewComponent(IRoleService roleService, IHttpContextAccessor httpContextAccessor)
        {
            _roleService = roleService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var tenantIdCookie = _httpContextAccessor.HttpContext?.Request.Cookies["TenantId"];
            Guid tenantId = Guid.Empty;
            
            if (!string.IsNullOrEmpty(tenantIdCookie) && Guid.TryParse(tenantIdCookie, out Guid parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
            
            var roles = await _roleService.GetAllRolesAsync(tenantId);
            return View(roles);
        }
    }
} 