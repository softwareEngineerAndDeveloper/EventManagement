using EventManagement.UI.Models.Auth;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventManagement.UI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly string _authEndpoint;

        public AuthService(IApiService apiService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _authEndpoint = _configuration["ApiSettings:Endpoints:Auth"] ?? "api/auth";
        }

        public async Task<ApiResponse<LoginResponseModel>> LoginAsync(LoginViewModel model)
        {
            Console.WriteLine($"Giriş denemesi: {model.Email}");
            
            // Subdomain set et (tenant için gerekli)
            model.Subdomain = _configuration["ApiSettings:DefaultTenant"] ?? "test";
            
            // API'den token al
            var response = await _apiService.PostAsync<object>($"{_authEndpoint}/login", model);
            
            if (response.Success && response.Data != null)
            {
                Console.WriteLine("API yanıtı başarılı");
                
                try
                {
                    // API yanıtını işle
                    string token = ExtractTokenFromResponse(response.Data);
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine($"Token alındı: {token.Substring(0, Math.Min(token.Length, 20))}...");
                        
                        // Token'dan kullanıcı bilgilerini çıkar
                        var loginResponse = new LoginResponseModel
                        {
                            Token = token,
                            ExpiresAt = DateTime.UtcNow.AddHours(24), // Token süresi varsayılan
                            UserId = ParseUserIdFromToken(token),
                            Email = model.Email,
                            Roles = ParseRolesFromToken(token),
                            TenantId = ParseTenantIdFromToken(token)
                        };
                        
                        Console.WriteLine($"Kullanıcı rolleri: {string.Join(", ", loginResponse.Roles)}");
                        Console.WriteLine($"Kullanıcı ID: {loginResponse.UserId}");
                        
                        await CreateAuthenticationCookieAsync(loginResponse);
                        
                        return new ApiResponse<LoginResponseModel>
                        {
                            Success = true,
                            Message = "Giriş başarılı",
                            Data = loginResponse,
                            StatusCode = response.StatusCode
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token işleme hatası: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            Console.WriteLine($"Giriş başarısız: {response.Message}");
            return new ApiResponse<LoginResponseModel>
            {
                Success = false,
                Message = response.Message,
                StatusCode = response.StatusCode
            };
        }

        private string ExtractTokenFromResponse(object responseData)
        {
            try
            {
                // Yanıt objesi direkt JSON string olarak gelirse
                if (responseData is string stringToken)
                {
                    return stringToken;
                }
                
                // Yanıt komplex bir obje olarak gelirse
                var jsonStr = JsonConvert.SerializeObject(responseData);
                var jObj = JObject.Parse(jsonStr);
                
                // API yanıt formatına göre token değerini bul
                if (jObj["data"] != null)
                {
                    var data = jObj["data"];
                    if (data is JValue) 
                    {
                        return data.ToString(); // Direkt token string'i
                    }
                    else if (data["token"] != null)
                    {
                        return data["token"].ToString(); // data.token formatı
                    }
                }
                else if (jObj["token"] != null)
                {
                    return jObj["token"].ToString(); // token formatı
                }
                
                // Başka bir format gelirse
                foreach (var prop in jObj.Properties())
                {
                    if (prop.Name.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token çıkarma hatası: {ex.Message}");
            }
            
            return string.Empty;
        }

        private Guid ParseUserIdFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                
                var nameIdClaim = jsonToken?.Claims.FirstOrDefault(c => 
                    c.Type == ClaimTypes.NameIdentifier ||
                    c.Type == "nameid" ||
                    c.Type == "sub" ||
                    c.Type == "userId");
                
                if (nameIdClaim != null && Guid.TryParse(nameIdClaim.Value, out Guid userId))
                {
                    return userId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserId parse hatası: {ex.Message}");
            }
            
            return Guid.NewGuid(); // Geçici ID oluştur
        }

        private List<string> ParseRolesFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                
                var roleClaims = jsonToken?.Claims.Where(c => 
                    c.Type == ClaimTypes.Role || 
                    c.Type == "role" ||
                    c.Type == "roles");
                
                if (roleClaims != null && roleClaims.Any())
                {
                    return roleClaims.Select(c => c.Value).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Roller parse hatası: {ex.Message}");
            }
            
            // Varsayılan olarak Admin rolü ver
            return new List<string> { "Admin" };
        }

        private Guid ParseTenantIdFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                
                var tenantIdClaim = jsonToken?.Claims.FirstOrDefault(c => 
                    c.Type == "TenantId" ||
                    c.Type == "tenantid" ||
                    c.Type == "tenant_id");
                
                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out Guid tenantId))
                {
                    return tenantId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TenantId parse hatası: {ex.Message}");
            }
            
            return Guid.NewGuid(); // Geçici ID oluştur
        }

        public async Task<ApiResponse<bool>> RegisterAsync(RegisterViewModel model)
        {
            return await _apiService.PostAsync<bool>($"{_authEndpoint}/register", model);
        }

        public string? GetTokenAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var tokenClaim = context.User.Claims.FirstOrDefault(c => c.Type == "Token");
            return tokenClaim?.Value;
        }

        public bool IsAuthenticatedAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<bool> LogoutAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return false;

            // Kimlik doğrulama çerezini sil
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Oturum verilerini temizle
            context.Session.Clear();
            
            // Tüm çerezleri temizle
            foreach (var cookie in context.Request.Cookies.Keys)
            {
                context.Response.Cookies.Delete(cookie);
            }
            
            return true;
        }

        public bool IsInRoleAsync(string role)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return false;

            return context.User.IsInRole(role);
        }

        public List<string> GetUserRolesAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return new List<string>();

            var roleClaims = context.User.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
            return roleClaims.Select(c => c.Value).ToList();
        }

        public Guid GetCurrentUserIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Guid.Empty;

            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Guid.Empty;
            }

            return userId;
        }

        public Guid GetCurrentTenantIdAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Guid.Empty;

            var tenantIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "TenantId");
            if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim.Value, out Guid tenantId))
            {
                return Guid.Empty;
            }

            return tenantId;
        }

        private async Task CreateAuthenticationCookieAsync(LoginResponseModel model)
        {
            Console.WriteLine($"Oturum oluşturuluyor. UserId: {model.UserId}, Email: {model.Email}, Roller: {string.Join(",", model.Roles)}");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, model.UserId.ToString()),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim("Token", model.Token),
                new Claim("TenantId", model.TenantId.ToString())
            };

            // Rolleri ekle
            foreach (var role in model.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = model.ExpiresAt
            };

            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                Console.WriteLine("HttpContext mevcut, SignInAsync çağrılıyor");
                
                // Token'ı session'a kaydet
                context.Session.SetString("AuthToken", model.Token);
                Console.WriteLine($"Token session'a kaydedildi: {model.Token.Substring(0, Math.Min(model.Token.Length, 20))}...");
                
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                Console.WriteLine("SignInAsync tamamlandı");
            }
            else
            {
                Console.WriteLine("HttpContext null, oturum oluşturulamadı");
            }
        }
    }
} 