using EventManagement.UI.Services;
using EventManagement.UI.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using EventManagement.UI.DTOs;
using EventManagement.UI.Models;

namespace EventManagement.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiServiceUI _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IApiServiceUI apiService, ILogger<AccountController> logger)
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
                                
                                // Admin veya EventManager rolündeki kullanıcıları Admin/Index'e yönlendir
                                Console.WriteLine($"Kullanıcı Admin rolüne sahip mi? {roles.Contains("Admin")}");
                                Console.WriteLine($"Kullanıcı EventManager rolüne sahip mi? {roles.Contains("EventManager")}");
                                
                                if (roles.Contains("Admin"))
                                {
                                    Console.WriteLine("Admin sayfasına yönlendiriliyor...");
                                    return RedirectToAction("Index", "Admin");
                                }
                                else if (roles.Contains("EventManager"))
                                {
                                    Console.WriteLine("EventManager sayfasına yönlendiriliyor...");
                                    return RedirectToAction("Manager", "Event");
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
            // 1. Cookie tabanlı kimlik doğrulamayı temizle
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // 2. Cookie'yi temizle
            Response.Cookies.Delete("jwt_token");
            
            // 3. Session'ı tamamen temizle
            HttpContext.Session.Clear();
            
            _logger.LogInformation("Kullanıcı çıkış yaptı");
            
            // 4. Login sayfasına yönlendir
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Session'dan UserID'yi al
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login");
                }

                // API'den mevcut kullanıcı bilgilerini çek
                var user = await _apiService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Kullanıcı bilgilerini içeren model oluştur
                var profileModel = new ProfileViewModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                return View(profileModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil bilgileri alınırken hata oluştu");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            try
            {
                // Update DTO oluştur
                var updateProfileDto = new UpdateProfileDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber
                };

                // API'ye güncelleme isteği gönder
                var result = await _apiService.UpdateCurrentUserAsync(updateProfileDto);
                if (result)
                {
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Profil güncellenirken bir hata oluştu.";
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil güncellenirken hata oluştu");
                TempData["ErrorMessage"] = $"Profil güncellenirken bir hata oluştu: {ex.Message}";
                return View("Profile", model);
            }
        }
    }
} 