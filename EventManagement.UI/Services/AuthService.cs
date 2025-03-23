using EventManagement.UI.Models.Auth;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.AccessControl;
using EventManagement.UI.Services.Interfaces;
using System.Text;

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
            _authEndpoint = _configuration["ApiEndpoints:Auth"] ?? "api/auth";
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
            try
            {
                // Doğrudan modeli post et, content dönüşümünü API servisi halledecek
                var response = await _apiService.PostAsync<bool>($"{_authEndpoint}/register", model);
                
                if (response.IsSuccess)
                {
                    return new ApiResponse<bool> { Success = true, Message = "Kayıt başarılı", Data = true };
                }
                
                return new ApiResponse<bool> { Success = false, Message = response.Message, Data = false };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = ex.Message, Data = false };
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Token")?.Value;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
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

        public async Task<bool> IsInRoleAsync(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        public async Task<List<string>> GetUserRolesAsync()
        {
            var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
            return roles;
        }

        public async Task<Guid> GetCurrentUserIdAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId != null ? Guid.Parse(userId) : Guid.Empty;
        }

        public async Task<Guid> GetCurrentTenantIdAsync()
        {
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return tenantId != null ? Guid.Parse(tenantId) : Guid.Empty;
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

        public async Task<ResponseModel<string>> LoginAsync(string email, string password)
        {
            try
            {
                var loginViewModel = new LoginViewModel
                {
                    Email = email,
                    Password = password
                };
                
                var response = await LoginAsync(loginViewModel);
                
                if (response.Success && response.Data != null)
                {
                    // Mevcut login metodundaki işlemleri yapıp token'ı döndür
                    return ResponseModel<string>.Success(response.Data.Token, "Giriş başarılı");
                }
                
                return ResponseModel<string>.Fail(response.Message ?? "Giriş yapılamadı");
            }
            catch (Exception ex)
            {
                return ResponseModel<string>.Fail($"Giriş yapılırken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<bool>> RegisterAsync(UserRegistrationViewModel model)
        {
            try
            {
                // RegisterViewModel'e dönüştür
                var registerViewModel = new RegisterViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    TenantId = model.TenantId.ToString() // Guid'i string'e dönüştür
                };
                
                var result = await RegisterAsync(registerViewModel);
                
                return new ResponseModel<bool>
                {
                    IsSuccess = result.Success,
                    Message = result.Message,
                    Data = result.Success
                };
            }
            catch (Exception ex)
            {
                return ResponseModel<bool>.Fail($"Kayıt yapılırken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<bool>> ChangePasswordAsync(ChangePasswordViewModel model, string token)
        {
            try
            {
                // Şifre değiştirme işlemini API'ye yap
                var endpoint = $"api/auth/change-password";
                var response = await _apiService.PostHttpResponseAsync(endpoint, model, token);
                
                if (response.IsSuccessStatusCode)
                {
                    return ResponseModel<bool>.Success(true, "Şifre başarıyla değiştirildi");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return ResponseModel<bool>.Fail($"Şifre değiştirilemedi: {content}");
            }
            catch (Exception ex)
            {
                return ResponseModel<bool>.Fail($"Şifre değiştirilirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<EventManagement.UI.Models.User.UserViewModel>> GetCurrentUserAsync(string token)
        {
            try
            {
                var endpoint = "api/users/me";
                var response = await _apiService.GetHttpResponseAsync(endpoint, token);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ApiWrapper<EventManagement.UI.Models.User.UserViewModel>>(content);
                    
                    if (result != null && result.IsSuccess)
                    {
                        return ResponseModel<EventManagement.UI.Models.User.UserViewModel>.Success(result.Data);
                    }
                    
                    return ResponseModel<EventManagement.UI.Models.User.UserViewModel>.Fail(result?.Message ?? "Kullanıcı bilgileri alınamadı");
                }
                
                return ResponseModel<EventManagement.UI.Models.User.UserViewModel>.Fail($"Kullanıcı bilgileri alınamadı: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return ResponseModel<EventManagement.UI.Models.User.UserViewModel>.Fail($"Kullanıcı bilgileri alınırken bir hata oluştu: {ex.Message}");
            }
        }

        private class ApiWrapper<T>
        {
            [JsonProperty("isSuccess")]
            public bool IsSuccess { get; set; }
            
            [JsonProperty("message")]
            public string Message { get; set; }
            
            [JsonProperty("data")]
            public T Data { get; set; }
        }
    }
} 