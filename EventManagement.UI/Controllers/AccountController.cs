using EventManagement.UI.Models.DTOs;
using EventManagement.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace EventManagement.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IApiService apiService, ILogger<AccountController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Console.WriteLine($"Login Attempt: {model.Email}, Subdomain: {model.Subdomain}");
                    var response = await _apiService.LoginAsync(model);

                    if (response != null && !string.IsNullOrEmpty(response.Token))
                    {
                        // Başarılı giriş
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var securityToken = tokenHandler.ReadToken(response.Token) as JwtSecurityToken;

                        if (securityToken != null)
                        {
                            try
                            {
                                // Tüm token içeriğini debug için yazdır
                                Console.WriteLine("TOKEN DEBUG: Token içeriği:");
                                foreach (var claim in securityToken.Claims)
                                {
                                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                                }

                                // Token bilgilerinden kullanıcı rollerini alma
                                var roles = securityToken.Claims.Where(c => c.Type == ClaimTypes.Role)
                                                        .Select(c => c.Value)
                                                        .ToList();

                                Console.WriteLine($"Token'dan alınan roller: {string.Join(", ", roles)}");

                                var userId = securityToken.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
                                var tenantId = securityToken.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value 
                                            ?? securityToken.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value;

                                Console.WriteLine($"UserId: {userId}, TenantId: {tenantId}");

                                // Cookie'ye token bilgisini ekle
                                Response.Cookies.Append("jwt_token", response.Token, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTime.Now.AddHours(1)
                                });

                                // Session'da roll bilgilerini sakla (eğer varsa)
                                if (roles.Any())
                                {
                                    HttpContext.Session.SetString("UserRoles", string.Join(",", roles));
                                }

                                // Diğer önemli bilgileri session'a kaydet
                                if (!string.IsNullOrEmpty(userId))
                                {
                                    HttpContext.Session.SetString("UserId", userId);
                                }

                                if (!string.IsNullOrEmpty(tenantId))
                                {
                                    HttpContext.Session.SetString("TenantId", tenantId);
                                }

                                // Kullanıcıyı cookie tabanlı kimlik doğrulaması ile giriş yaptır
                                var claims = new List<Claim>();
                                foreach(var claim in securityToken.Claims)
                                {
                                    // Role bilgilerini eklerken ClaimTypes.Role kullanıyoruz
                                    if (claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                                    {
                                        claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                                    }
                                    else 
                                    {
                                        claims.Add(new Claim(claim.Type, claim.Value));
                                    }
                                }

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                                };

                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);

                                Console.WriteLine("Login başarılı, kullanıcı yönlendiriliyor");
                                
                                // Admin rolüne sahip kullanıcıyı Admin sayfasına yönlendir
                                Console.WriteLine($"Kullanıcı Admin rolüne sahip mi? {roles.Contains("Admin")}");
                                if (roles.Contains("Admin"))
                                {
                                    Console.WriteLine("Admin sayfasına yönlendiriliyor...");
                                    return RedirectToAction("Index", "Admin");
                                }
                                
                                Console.WriteLine("Ana sayfaya yönlendiriliyor...");
                                return RedirectToAction("Index", "Home");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Token işleme hatası: {ex.Message}");
                                ModelState.AddModelError("", $"Token işlenirken hata oluştu: {ex.Message}");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Geçersiz token formatı");
                            Console.WriteLine("Geçersiz token formatı");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "API'den geçerli bir token alınamadı. Lütfen giriş bilgilerinizi kontrol edin.");
                        Console.WriteLine("API'den geçerli bir token alınamadı.");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Giriş işlemi sırasında bir hata oluştu: {ex.Message}");
                Console.WriteLine($"Login error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (ModelState.IsValid)
            {
                var result = await _apiService.RegisterAsync(registerDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Kullanıcı başarıyla kaydoldu: {Email}", registerDto.Email);
                    return RedirectToAction(nameof(Login));
                }
                
                ModelState.AddModelError(string.Empty, result.Message);
            }
            
            return View(registerDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Token cookie'sini temizle
            Response.Cookies.Delete("jwt_token");
            
            _logger.LogInformation("Kullanıcı çıkış yaptı");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("tenant_id", user.TenantId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
} 