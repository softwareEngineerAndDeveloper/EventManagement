using EventManagement.UI.DTOs;
using EventManagement.UI.Interfaces;
using EventManagement.UI.Models;
using EventManagement.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventManagement.UI.Controllers
{
    // Controller seviyesinde authorization kaldırıldı
    public class AdminController : Controller
    {
        private readonly IEventServiceUI _eventService;
        private readonly IUserServiceUI _userService;
        private readonly IRoleServiceUI _roleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public AdminController(IEventServiceUI eventService, IUserServiceUI userService, IRoleServiceUI roleService, IHttpContextAccessor httpContextAccessor)
        {
            _eventService = eventService;
            _userService = userService;
            _roleService = roleService;
            _httpContextAccessor = httpContextAccessor;
        }
        
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> Index()
        {
            // Ana sayfada onay bekleyen etkinlikleri listele
            var tenantId = GetTenantIdFromCookie();
            var pendingEvents = await _eventService.GetPendingEventsAsync(tenantId);
            
            return View(pendingEvents);
        }
        
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> Events()
        {
            var tenantId = GetTenantIdFromCookie();
            Console.WriteLine($"Admin/Events: Tenant ID = {tenantId}");
            
            var events = await _eventService.GetAllEventsAsync(tenantId);
            Console.WriteLine($"Admin/Events: {events.Count} etkinlik bulundu");
            
            // Boş liste veya null olma durumuna karşı koruma
            if (events == null)
            {
                events = new List<EventDto>();
                Console.WriteLine("Admin/Events: events null olduğu için boş liste oluşturuldu");
            }
            
            return View(events);
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var tenantId = GetTenantIdFromCookie();
            var users = await _userService.GetAllUsersAsync(tenantId);
            
            return View(users);
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Roles()
        {
            var tenantId = GetTenantIdFromCookie();
            var roles = await _roleService.GetAllRolesAsync(tenantId);
            
            return View(roles);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,EventManager")]
        public async Task<IActionResult> CancelEvent(Guid id)
        {
            var tenantId = GetTenantIdFromCookie();
            await _eventService.CancelEventAsync(id, tenantId);
            
            return RedirectToAction(nameof(Events));
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(Guid id)
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
                    IsActive = true
                };
                
                await _userService.UpdateUserAsync(id, updateUserDto, tenantId);
            }
            
            return RedirectToAction(nameof(Users));
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser()
        {
            return View();
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(RegisterDto model)
        {
            if (ModelState.IsValid)
            {
                var tenantId = GetTenantIdFromCookie();
                await _userService.CreateUserAsync(model, tenantId);
                return RedirectToAction(nameof(Users));
            }
            
            return View(model);
        }
        
        private Guid GetTenantIdFromCookie()
        {
            // Önce cookie'den TenantId'yi almayı deneyelim
            var tenantIdCookie = _httpContextAccessor.HttpContext?.Request.Cookies["TenantId"];
            if (!string.IsNullOrEmpty(tenantIdCookie) && Guid.TryParse(tenantIdCookie, out Guid tenantIdFromCookie))
            {
                Console.WriteLine($"TenantId cookie'den alındı: {tenantIdFromCookie}");
                return tenantIdFromCookie;
            }
            
            // Cookie'de yoksa session'dan almayı deneyelim
            var tenantIdSession = _httpContextAccessor.HttpContext?.Session.GetString("TenantId");
            if (!string.IsNullOrEmpty(tenantIdSession) && Guid.TryParse(tenantIdSession, out Guid tenantIdFromSession))
            {
                Console.WriteLine($"TenantId session'dan alındı: {tenantIdFromSession}");
                return tenantIdFromSession;
            }
            
            // Son çare olarak User.Claims'den almayı deneyelim
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = user.Claims.FirstOrDefault(c => c.Type == "tenant_id" || c.Type == "TenantId");
                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out Guid tenantIdFromClaim))
                {
                    Console.WriteLine($"TenantId claim'den alındı: {tenantIdFromClaim}");
                    return tenantIdFromClaim;
                }
            }
            
            // Hiçbir yerden alınamazsa sabit bir değer kullanabiliriz
            var defaultTenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"); // Test tenant için sabit GUID
            Console.WriteLine($"TenantId bulunamadı, varsayılan değer kullanılıyor: {defaultTenantId}");
            return defaultTenantId;
        }
    }
} 