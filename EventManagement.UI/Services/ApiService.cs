using EventManagement.UI.Models.DTOs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace EventManagement.UI.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<ApiService> _logger;

        public ApiService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IOptions<ApiSettings> apiSettings,
            ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _apiSettings = apiSettings.Value;
            _logger = logger;

            // Base URL ayarlanıyor
            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
            
            // Cookie'lerden token ve tenant bilgisini al
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
            var tenant = _httpContextAccessor.HttpContext?.Request.Cookies["tenant"] ?? _apiSettings.DefaultTenant;
            
            // Tenant bilgisi yoksa varsayılan değeri kullan
            if (string.IsNullOrEmpty(tenant))
            {
                tenant = "test"; // Varsayılan tenant değeri
            }
            
            // Token varsa Authorization header'ını ayarla
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            // Tenant bilgisi varsa X-Tenant header'ını ayarla
            if (!string.IsNullOrEmpty(tenant))
            {
                // Önceden varsa header'ı temizle
                if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
                }
                _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenant);
                _logger.LogInformation("X-Tenant header'ı ayarlandı: {Tenant}", tenant);
            }
            else
            {
                _logger.LogWarning("X-Tenant header'ı için tenant bilgisi bulunamadı");
            }
        }

        #region Auth Methods
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                Console.WriteLine($"Login isteği hazırlanıyor: Email={loginDto.Email}, Subdomain={loginDto.Subdomain}");
                
                var content = CreateJsonContent(loginDto);
                
                // İstek içeriğini debug için yazdıralım
                var requestBody = await content.ReadAsStringAsync();
                Console.WriteLine($"API'ye gönderilecek istek: {requestBody}");
                
                // X-Tenant header'ı kontrol ve eklemesi yapalım
                if (!string.IsNullOrEmpty(loginDto.Subdomain))
                {
                    if (!_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
                    {
                        _httpClient.DefaultRequestHeaders.Add("X-Tenant", loginDto.Subdomain);
                        Console.WriteLine($"X-Tenant header'ı eklendi: {loginDto.Subdomain}");
                    }
                }
                
                // Debug için tüm headers'ları yazdıralım
                Console.WriteLine("Gönderilen Headers:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                Console.WriteLine($"API URL: {_httpClient.BaseAddress}{_apiSettings.Endpoints.Auth}/login");
                
                try 
                {
                    var response = await _httpClient.PostAsync($"{_apiSettings.Endpoints.Auth}/login", content);
                    
                    Console.WriteLine($"API yanıt kodu: {response.StatusCode}");
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API yanıt içeriği: {responseContent}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // API yanıtı farklı formatta geliyor, önce bunu analiz edelim
                        var apiResponse = JsonConvert.DeserializeObject<ResponseDto<string>>(responseContent);
                        
                        if (apiResponse != null && apiResponse.IsSuccess && !string.IsNullOrEmpty(apiResponse.Data))
                        {
                            // Token'ı almışız, AuthResponseDto nesnesini oluşturalım
                            return new AuthResponseDto 
                            { 
                                IsSuccess = true, 
                                Message = "Giriş başarılı", 
                                Token = apiResponse.Data 
                            };
                        }
                        else
                        {
                            Console.WriteLine($"API yanıtı başarılı ancak token alınamadı: {responseContent}");
                            return new AuthResponseDto { IsSuccess = false, Message = "Token alınamadı" };
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Login başarısız. Durum kodu: {response.StatusCode}, Yanıt: {responseContent}");
                        return new AuthResponseDto { IsSuccess = false, Message = $"Giriş başarısız: {response.StatusCode}" };
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP istek hatası: {httpEx.Message}");
                    Console.WriteLine($"HTTP istek iç detayları: {httpEx.InnerException?.Message}");
                    return new AuthResponseDto { IsSuccess = false, Message = $"HTTP Hatası: {httpEx.Message}" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"İç hata: {ex.InnerException.Message}");
                }
                return new AuthResponseDto { IsSuccess = false, Message = $"Hata: {ex.Message}" };
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var content = CreateJsonContent(registerDto);
            var response = await _httpClient.PostAsync($"{_apiSettings.Endpoints.Auth}/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<AuthResponseDto>>(responseContent);
                
                if (result?.IsSuccess == true && result.Data != null)
                {
                    return result.Data;
                }
                
                return new AuthResponseDto { IsSuccess = false, Message = result?.Message ?? "Kayıt başarısız" };
            }
            
            return new AuthResponseDto { IsSuccess = false, Message = "Kayıt başarısız: " + response.ReasonPhrase };
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Users}/me");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<UserDto>>(responseContent);
                
                if (result?.IsSuccess == true)
                {
                    return result.Data;
                }
            }
            
            return null;
        }
        #endregion

        #region Event Methods
        public async Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(EventFilterDto? filter = null)
        {
            var queryString = string.Empty;
            if (filter != null)
            {
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    queryParams.Add($"SearchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
                
                if (filter.StartDateFrom.HasValue)
                    queryParams.Add($"StartDateFrom={filter.StartDateFrom.Value:yyyy-MM-dd}");
                
                if (filter.StartDateTo.HasValue)
                    queryParams.Add($"StartDateTo={filter.StartDateTo.Value:yyyy-MM-dd}");
                
                if (filter.IsActive.HasValue)
                    queryParams.Add($"IsActive={filter.IsActive.Value}");
                
                if (queryParams.Any())
                    queryString = "?" + string.Join("&", queryParams);
            }
            
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<EventDto>>>(responseContent);
                
                return result ?? new ResponseDto<List<EventDto>> { IsSuccess = false, Message = "Veri alınamadı", Data = new List<EventDto>() };
            }
            
            return new ResponseDto<List<EventDto>> { IsSuccess = false, Message = $"Etkinlikler alınamadı: {response.ReasonPhrase}", Data = new List<EventDto>() };
        }

        public async Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                return result ?? new ResponseDto<EventDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<EventDto> { IsSuccess = false, Message = $"Etkinlik alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto)
        {
            var content = CreateJsonContent(createEventDto);
            var response = await _httpClient.PostAsync(_apiSettings.Endpoints.Events, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                return result ?? new ResponseDto<EventDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<EventDto> { IsSuccess = false, Message = $"Etkinlik oluşturulamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto)
        {
            var content = CreateJsonContent(updateEventDto);
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Events}/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                return result ?? new ResponseDto<EventDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<EventDto> { IsSuccess = false, Message = $"Etkinlik güncellenemedi: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<bool>> DeleteEventAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints.Events}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                return result ?? new ResponseDto<bool> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<bool> { IsSuccess = false, Message = $"Etkinlik silinemedi: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{id}/statistics");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventStatisticsDto>>(responseContent);
                
                return result ?? new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = $"Etkinlik istatistikleri alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/upcoming");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<EventDto>>>(responseContent);
                
                return result ?? new ResponseDto<List<EventDto>> { IsSuccess = false, Message = "Veri alınamadı", Data = new List<EventDto>() };
            }
            
            return new ResponseDto<List<EventDto>> { IsSuccess = false, Message = $"Yaklaşan etkinlikler alınamadı: {response.ReasonPhrase}", Data = new List<EventDto>() };
        }
        #endregion

        #region Registration Methods
        public async Task<ResponseDto<List<RegistrationDto>>> GetRegistrationsByEventIdAsync(Guid eventId)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{eventId}/registrations");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<RegistrationDto>>>(responseContent);
                
                return result ?? new ResponseDto<List<RegistrationDto>> { IsSuccess = false, Message = "Veri alınamadı", Data = new List<RegistrationDto>() };
            }
            
            return new ResponseDto<List<RegistrationDto>> { IsSuccess = false, Message = $"Kayıtlar alınamadı: {response.ReasonPhrase}", Data = new List<RegistrationDto>() };
        }

        public async Task<ResponseDto<RegistrationDto>> GetRegistrationByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Registrations}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<RegistrationDto>>(responseContent);
                
                return result ?? new ResponseDto<RegistrationDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<RegistrationDto> { IsSuccess = false, Message = $"Kayıt alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<RegistrationDto>> CreateRegistrationAsync(Guid eventId, CreateRegistrationDto createRegistrationDto)
        {
            var content = CreateJsonContent(createRegistrationDto);
            var response = await _httpClient.PostAsync($"{_apiSettings.Endpoints.Events}/{eventId}/registrations", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<RegistrationDto>>(responseContent);
                
                return result ?? new ResponseDto<RegistrationDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<RegistrationDto> { IsSuccess = false, Message = $"Kayıt oluşturulamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<RegistrationDto>> UpdateRegistrationAsync(Guid eventId, Guid id, UpdateRegistrationDto updateRegistrationDto)
        {
            var content = CreateJsonContent(updateRegistrationDto);
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Events}/{eventId}/registrations/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<RegistrationDto>>(responseContent);
                
                return result ?? new ResponseDto<RegistrationDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<RegistrationDto> { IsSuccess = false, Message = $"Kayıt güncellenemedi: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<bool>> DeleteRegistrationAsync(Guid eventId, Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints.Events}/{eventId}/registrations/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                return result ?? new ResponseDto<bool> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<bool> { IsSuccess = false, Message = $"Kayıt silinemedi: {response.ReasonPhrase}" };
        }
        #endregion

        #region Tenant Methods
        public async Task<ResponseDto<TenantDto>> GetCurrentTenantAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Tenants}/current");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<TenantDto>>(responseContent);
                
                return result ?? new ResponseDto<TenantDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<TenantDto> { IsSuccess = false, Message = $"Tenant alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<TenantDto>> GetTenantByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Tenants}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<TenantDto>>(responseContent);
                
                return result ?? new ResponseDto<TenantDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<TenantDto> { IsSuccess = false, Message = $"Tenant alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto)
        {
            var content = CreateJsonContent(createTenantDto);
            var response = await _httpClient.PostAsync(_apiSettings.Endpoints.Tenants, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<TenantDto>>(responseContent);
                
                if (result?.IsSuccess == true && result.Data != null)
                {
                    // Tenant bilgisini cookie'ye kaydet
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append("tenant", result.Data.Subdomain, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.Now.AddYears(1)
                    });
                }
                
                return result ?? new ResponseDto<TenantDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<TenantDto> { IsSuccess = false, Message = $"Tenant oluşturulamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantDto updateTenantDto)
        {
            var content = CreateJsonContent(updateTenantDto);
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Tenants}/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<TenantDto>>(responseContent);
                
                return result ?? new ResponseDto<TenantDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<TenantDto> { IsSuccess = false, Message = $"Tenant güncellenemedi: {response.ReasonPhrase}" };
        }
        #endregion

        #region Helpers
        private HttpContent CreateJsonContent<T>(T data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Content-Type header'ını açıkça ayarlayalım
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            return content;
        }
        #endregion
    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public ApiEndpoints Endpoints { get; set; } = new ApiEndpoints();
        public string DefaultTenant { get; set; } = string.Empty;
    }

    public class ApiEndpoints
    {
        public string Auth { get; set; } = string.Empty;
        public string Events { get; set; } = string.Empty;
        public string Tenants { get; set; } = string.Empty;
        public string Users { get; set; } = string.Empty;
        public string Registrations { get; set; } = string.Empty;
    }
} 