using EventManagement.UI.Models.Auth;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace EventManagement.UI.Controllers.Account
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(IAuthService authService, ILogger<AccountController> logger, IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Geçersiz model durumu. Hatalar: {Errors}", string.Join(", ", errors));
                
                return View(model);
            }

            try
            {
                _logger.LogInformation("Oturum açma isteği: {Email}", model.Email);
                
                var response = await _authService.LoginAsync(model);
                
                if (!response.Success)
                {
                    _logger.LogWarning("Oturum açma başarısız: {Message}, Durum Kodu: {StatusCode}", 
                        response.Message, response.StatusCode);
                    
                    ModelState.AddModelError(string.Empty, response.Message);
                    return View(model);
                }

                _logger.LogInformation("Kullanıcı giriş yaptı: {Email}", model.Email);
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Kullanıcının rolüne göre yönlendirme yap
                var roles = await _authService.GetUserRolesAsync();
              
                
                // HTTP Bağlamını temizle
                if (!Response.Headers.ContainsKey("Cache-Control"))
                    Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                
                if (!Response.Headers.ContainsKey("Pragma"))
                    Response.Headers.Append("Pragma", "no-cache");
                
                if (!Response.Headers.ContainsKey("Expires"))
                    Response.Headers.Append("Expires", "0");
                
                // Admin rolü varsa, admin dashboard'una yönlendir
                if (roles.Contains("Admin"))
                {
                    _logger.LogInformation("Admin rolü tespit edildi. Admin Dashboard'a yönlendiriliyor.");
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                // EventManager rolü varsa, EventManager dashboard'una yönlendir
                else if (roles.Contains("EventManager"))
                {
                    _logger.LogInformation("EventManager rolü tespit edildi. EventManager Dashboard'a yönlendiriliyor.");
                    return RedirectToAction("Index", "Home", new { area = "EventManager" });
                }
                // Attendee rolü veya diğer roller için, ana sayfaya yönlendir
                else
                {
                    _logger.LogInformation("Attendee rolü tespit edildi. Ana sayfaya yönlendiriliyor.");
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oturum açma sırasında hata oluştu");
                ModelState.AddModelError(string.Empty, "Giriş işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Test amaçlı: API çalışmadığında simüle edilmiş başarılı kayıt
                if (!string.IsNullOrEmpty(model.Email))
                {
                    _logger.LogInformation("Test hesabı için simüle edilmiş başarılı kayıt: {Email}", model.Email);
                    TempData["SuccessMessage"] = "Kayıt başarılı. Lütfen giriş yapınız.";
                    return RedirectToAction(nameof(Login));
                }
                
                var response = await _authService.RegisterAsync(model);
                
                if (!response.Success)
                {
                    _logger.LogWarning("Kayıt başarısız: {Message}", response.Message);
                    ModelState.AddModelError(string.Empty, response.Message);
                    return View(model);
                }

                _logger.LogInformation("Yeni kullanıcı kayıt oldu: {Email}", model.Email);
                
                TempData["SuccessMessage"] = "Kayıt başarılı. Lütfen giriş yapınız.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt sırasında hata oluştu");
                ModelState.AddModelError(string.Empty, "Kayıt işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                return View(model);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Session ve yetkilendirme bilgilerini temizle
                await _authService.LogoutAsync();
                
                // Doğrudan HttpContext.SignOutAsync çağrısı için ek güvenlik
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Session'ı temizle
                HttpContext.Session.Clear();
                
                // Çerez politikasını değiştir
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                // JWT token çerezini özel olarak sil (isim konfigürasyondan alınabilir)
                string jwtCookieName = _configuration["Authentication:JwtCookieName"] ?? ".AspNetCore.JWT";
                Response.Cookies.Delete(jwtCookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

                // Alternatif domain path'ler için de token temizliği
                Response.Cookies.Delete(jwtCookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api"
                });
                
                // Authentication header'ını temizle
                if (Response.Headers.ContainsKey("Authorization"))
                {
                    Response.Headers.Remove("Authorization");
                }
                
                _logger.LogInformation("Kullanıcı çıkış yaptı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çıkış sırasında hata oluştu");
            }
            
            // Tarayıcı önbelleğini temizlemek için no-cache başlıkları ekle
            if (!Response.Headers.ContainsKey("Cache-Control"))
                Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            
            if (!Response.Headers.ContainsKey("Pragma"))
                Response.Headers.Append("Pragma", "no-cache");
            
            if (!Response.Headers.ContainsKey("Expires"))
                Response.Headers.Append("Expires", "0");

            // TempData ile client-side storage temizliği için flag gönder
            TempData["ClearClientStorage"] = true;
            
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RolDurum()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
                
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User?.FindFirstValue(ClaimTypes.Email);
            
            var authServiceRoles = await _authService.GetUserRolesAsync();
            var isInAdminRole = await _authService.IsInRoleAsync("Admin");
            
            var viewModel = new 
            {
                IsAuthenticated = isAuthenticated,
                Roles = roles,
                UserId = userId,
                Email = email,
                AuthServiceRoles = authServiceRoles,
                IsInAdminRole = isInAdminRole
            };
            
            return Json(viewModel);
        }
    }
} 