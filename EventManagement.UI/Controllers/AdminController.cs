using EventManagement.UI.Models;
using EventManagement.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventManagement.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public AdminController(IEventService eventService, IUserService userService, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
        {
            _eventService = eventService;
            _userService = userService;
            _roleService = roleService;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public async Task<IActionResult> Index()
        {
            // Ana sayfada onay bekleyen etkinlikleri listele
            var tenantId = GetTenantIdFromCookie();
            var pendingEvents = await _eventService.GetPendingEventsAsync(tenantId);
            
            return View(pendingEvents);
        }
        
        public async Task<IActionResult> Events()
        {
            var tenantId = GetTenantIdFromCookie();
            var events = await _eventService.GetAllEventsAsync(tenantId);
            
            return View(events);
        }
        
        public async Task<IActionResult> Users()
        {
            var tenantId = GetTenantIdFromCookie();
            var users = await _userService.GetAllUsersAsync(tenantId);
            
            return View(users);
        }
        
        public async Task<IActionResult> Roles()
        {
            var tenantId = GetTenantIdFromCookie();
            var roles = await _roleService.GetAllRolesAsync(tenantId);
            
            return View(roles);
        }
        
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(Guid id)
        {
            var tenantId = GetTenantIdFromCookie();
            await _eventService.ApproveEventAsync(id, tenantId);
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public async Task<IActionResult> RejectEvent(Guid id)
        {
            var tenantId = GetTenantIdFromCookie();
            await _eventService.RejectEventAsync(id, tenantId);
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public async Task<IActionResult> CancelEvent(Guid id)
        {
            var tenantId = GetTenantIdFromCookie();
            await _eventService.CancelEventAsync(id, tenantId);
            
            return RedirectToAction(nameof(Events));
        }
        
        [HttpPost]
        public async Task<IActionResult> AssignRole(Guid userId, Guid roleId)
        {
            var tenantId = GetTenantIdFromCookie();
            var assignRoleDto = new AssignRoleDto
            {
                UserId = userId,
                RoleId = roleId
            };
            
            await _userService.AssignRoleToUserAsync(assignRoleDto, tenantId);
            
            return RedirectToAction(nameof(Users));
        }
        
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var tenantId = GetTenantIdFromCookie();
            var user = await _userService.GetUserByIdAsync(id, tenantId);
            
            if (user != null)
            {
                var updateUserDto = new UpdateUserDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = false
                };
                
                await _userService.UpdateUserAsync(id, updateUserDto, tenantId);
            }
            
            return RedirectToAction(nameof(Users));
        }
        
        private Guid GetTenantIdFromCookie()
        {
            var tenantIdCookie = _httpContextAccessor.HttpContext?.Request.Cookies["TenantId"];
            if (Guid.TryParse(tenantIdCookie, out Guid tenantId))
            {
                return tenantId;
            }
            
            // Varsayılan değer veya hata işleme
            return Guid.Empty;
        }
    }
} 