using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventManagement.UI.Services.Interfaces;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // Geçici olarak sadece giriş yapma kontrolü yap, rol kontrolünü manuel yapalım
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IAuthService _authService;

        public DashboardController(ILogger<DashboardController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public IActionResult Index()
        {
            // Manuel rol kontrolü yap
            var roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
                
            _logger.LogInformation("Admin Dashboard görüntülendi - Roller: {Roles}", string.Join(", ", roles));
            
            // Bu kontrolü geçici olarak devre dışı bırakalım
            /*
            if (!roles.Contains("Admin"))
            {
                _logger.LogWarning("Yetkisiz erişim girişimi!");
                return RedirectToAction("AccessDenied", "Account", new { area = "" });
            }
            */
            
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }
        
        [AllowAnonymous]
        public IActionResult Teshis()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
                
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User?.FindFirstValue(ClaimTypes.Email);
            
            var viewModel = new 
            {
                IsAuthenticated = isAuthenticated,
                Roles = roles,
                UserId = userId,
                Email = email,
                ServiceRoles = _authService.GetUserRolesAsync(),
                IsInAdminRole = _authService.IsInRoleAsync("Admin"),
                RequestPath = HttpContext.Request.Path,
                RequestQuery = HttpContext.Request.QueryString.ToString(),
                RequestMethod = HttpContext.Request.Method,
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };
            
            return Json(viewModel);
        }
    }
} 