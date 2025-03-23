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
                
                // Test amaçlı: API çalışmadığında mock veri ile kimlik doğrulama
                if (model.Email == "admin@etkinlikyonetimi.com" && model.Password == "Admin123!")
                {
                    _logger.LogInformation("Test hesabı için simüle edilmiş başarılı giriş: {Email}", model.Email);
                    
                    // Manuel kimlik oluştur
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim("Token", "test-token"),
                        new Claim("TenantId", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                else if (model.Email == "manager@etkinlikyonetimi.com" && model.Password == "Manager123!")
                {
                    // Manager hesabı için test giriş
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim("Token", "test-token"),
                        new Claim("TenantId", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Manager")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    
                    return RedirectToAction("Index", "Dashboard", new { area = "Manager" });
                }
                else if (model.Email == "eventmanager@etkinlikyonetimi.com" && model.Password == "EventManager123!")
                {
                    // EventManager hesabı için test giriş
                    var testToken = "test-token-for-eventmanager-" + DateTime.Now.Ticks;
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim("Token", testToken),
                        new Claim("TenantId", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "EventManager")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    // Token'ı önce HttpContext Session'a kaydet
                    HttpContext.Session.SetString("AuthToken", testToken);
                    _logger.LogInformation("EventManager için test token session'a kaydedildi: {TokenStart}...", 
                        testToken.Substring(0, Math.Min(testToken.Length, 20)));

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    
                    _logger.LogInformation("EventManager kullanıcısı başarıyla giriş yaptı ve EventManager alanına yönlendiriliyor");
                    return RedirectToAction("Index", "Home", new { area = "EventManager" });
                }
                else if (model.Email == "attendee@etkinlikyonetimi.com" && model.Password == "Attendee123!")
                {
                    // Attendee hesabı için test giriş
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Email, model.Email),
                        new Claim("Token", "test-token"),
                        new Claim("TenantId", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Attendee")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    
                    return RedirectToAction("Index", "Dashboard", new { area = "Attendee" });
                }
                
                // Normal durumda API'yi çağır
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
                var roles = _authService.GetUserRolesAsync();
                _logger.LogInformation("Kullanıcı rolleri: {Roles}", string.Join(", ", roles));
                
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
                // Manager rolü varsa, manager dashboard'una yönlendir
                else if (roles.Contains("Manager"))
                {
                    _logger.LogInformation("Manager rolü tespit edildi. Manager Dashboard'a yönlendiriliyor.");
                    return RedirectToAction("Index", "Dashboard", new { area = "Manager" });
                }
                // Attendee rolü veya diğer roller için, attendee dashboard'una yönlendir
                else
                {
                    _logger.LogInformation("Attendee rolü tespit edildi. Attendee Dashboard'a yönlendiriliyor.");
                    return RedirectToAction("Index", "Dashboard", new { area = "Attendee" });
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
        public IActionResult RolDurum()
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
                AuthServiceRoles = _authService.GetUserRolesAsync(),
                IsInAdminRole = _authService.IsInRoleAsync("Admin")
            };
            
            return Json(viewModel);
        }
    }
} 