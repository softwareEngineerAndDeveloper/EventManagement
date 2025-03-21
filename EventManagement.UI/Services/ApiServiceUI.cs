using EventManagement.UI.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using EventManagement.UI.Models;
using EventManagement.UI.DTOs;

namespace EventManagement.UI.Services
{
    public class ApiServiceUI : IApiServiceUI
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Models.ApiSettings _apiSettings;
        private readonly ILogger<ApiServiceUI> _logger;

        public ApiServiceUI(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IOptions<Models.ApiSettings> apiSettings,
            ILogger<ApiServiceUI> logger)
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

        public async Task<bool> UpdateCurrentUserAsync(UpdateProfileDto updateProfileDto)
        {
            var content = CreateJsonContent(updateProfileDto);
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Users}/me", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                return result?.IsSuccess == true;
            }
            
            return false;
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                // Sadece yerel cookie ve session'ları temizleme işlemi yeterli olabilir
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout işlemi sırasında hata oluştu");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Auth}/change-password";
                
                // İsteği oluştur
                var content = CreateJsonContent(changePasswordDto);
                
                // İsteği gönder
                var response = await _httpClient.PostAsync(endpoint, content);
                
                // Başarılı mı kontrol et
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                _logger.LogWarning("Şifre değiştirme başarısız: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirme sırasında hata oluştu");
                return false;
            }
        }
        #endregion

        #region Event Methods
        public async Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            return await GetAllEventsAsync();
        }

        public async Task<ResponseDto<List<EventDto>>> GetAllEventsAsync(EventFilterDto? filter = null, Guid? tenantId = null)
        {
            try
            {
                // Tenant ID varsa header'ı güncelle
                if (tenantId.HasValue)
                {
                    if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
                    }
                    
                    _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.Value.ToString());
                    _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId.Value}");
                }
                
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
                
                var requestUrl = $"{_apiSettings.Endpoints.Events}{queryString}";
                _logger.LogInformation($"Etkinlik listesi için istek yapılıyor: {requestUrl}");
                
                // Headers'ları kontrol et
                var hasAuthHeader = _httpClient.DefaultRequestHeaders.Authorization != null;
                var hasTenantHeader = _httpClient.DefaultRequestHeaders.Contains("X-Tenant");
                _logger.LogInformation($"Headers - Authorization: {hasAuthHeader}, X-Tenant: {hasTenantHeader}");
                
                if (!hasTenantHeader)
                {
                    var tenant = _httpContextAccessor.HttpContext?.Request.Cookies["tenant"] ?? _apiSettings.DefaultTenant;
                    if (!string.IsNullOrEmpty(tenant))
                    {
                        _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenant);
                        _logger.LogInformation($"X-Tenant header'ı eklendi: {tenant}");
                    }
                }
                
                var response = await _httpClient.GetAsync(requestUrl);
                
                _logger.LogInformation($"API Yanıtı - Status: {response.StatusCode}, IsSuccessStatusCode: {response.IsSuccessStatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Yanıt içeriği: {responseContent}");
                    
                    var result = JsonConvert.DeserializeObject<ResponseDto<List<EventDto>>>(responseContent);
                    
                    if (result != null)
                    {
                        _logger.LogInformation($"Deserialize edilen etkinlik sayısı: {result.Data?.Count ?? 0}");
                        return result;
                    }
                    else
                    {
                        _logger.LogWarning("API yanıtı deserialize edilemedi");
                        return new ResponseDto<List<EventDto>> { IsSuccess = false, Message = "API yanıtı deserialize edilemedi", Data = new List<EventDto>() };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API hatası: {response.StatusCode} - {errorContent}");
                    
                    return new ResponseDto<List<EventDto>> 
                    { 
                        IsSuccess = false, 
                        Message = $"Etkinlikler alınamadı: {response.StatusCode} - {response.ReasonPhrase}", 
                        Data = new List<EventDto>() 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik listesi alınırken hata oluştu: {ex.Message}");
                return new ResponseDto<List<EventDto>> 
                { 
                    IsSuccess = false, 
                    Message = $"Etkinlik listesi alınırken bir hata oluştu: {ex.Message}", 
                    Data = new List<EventDto>() 
                };
            }
        }

        public async Task<ResponseDto<EventDto>> GetEventByIdAsync(Guid id, Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                if (result != null)
                {
                    return result;
                }
            }
            
            return new ResponseDto<EventDto> { IsSuccess = false, Message = "Etkinlik bulunamadı" };
        }

        public async Task<ResponseDto<EventDto>> CreateEventAsync(CreateEventDto createEventDto, Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var content = CreateJsonContent(createEventDto);
            var response = await _httpClient.PostAsync(_apiSettings.Endpoints.Events, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                if (result != null)
                {
                    return result;
                }
            }
            
            return new ResponseDto<EventDto> { IsSuccess = false, Message = "Etkinlik oluşturulamadı" };
        }

        public async Task<ResponseDto<EventDto>> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var content = CreateJsonContent(updateEventDto);
            
            // İstek içeriğini loglayalım
            var requestBody = await content.ReadAsStringAsync();
            _logger.LogInformation($"UpdateEvent isteği: EventId={id}, TenantId={tenantId}, Body={requestBody}");
            Console.WriteLine($"UpdateEvent isteği: EventId={id}, Status={updateEventDto.Status}, IsActive={updateEventDto.IsActive}");
            
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Events}/{id}", content);
            
            // Yanıtı loglayalım
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"UpdateEvent yanıtı: StatusCode={response.StatusCode}, Content={responseContent}");
            Console.WriteLine($"UpdateEvent yanıtı: StatusCode={response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<ResponseDto<EventDto>>(responseContent);
                
                if (result != null)
                {
                    // Başarılı yanıtı loglayalım
                    if (result.IsSuccess && result.Data != null)
                    {
                        _logger.LogInformation($"UpdateEvent başarılı: EventId={id}, YeniStatus={result.Data.Status}, YeniIsActive={result.Data.IsActive}");
                        Console.WriteLine($"UpdateEvent başarılı: EventId={id}, YeniStatus={result.Data.Status}, YeniIsActive={result.Data.IsActive}");
                    }
                    else
                    {
                        _logger.LogWarning($"API yanıtı başarılı ancak etkinlik güncellenemedi: {result.Message}");
                        Console.WriteLine($"API yanıtı başarılı ancak etkinlik güncellenemedi: {result.Message}");
                    }
                    
                    return result;
                }
            }
            
            _logger.LogError($"Etkinlik güncellenemedi: EventId={id}, StatusCode={response.StatusCode}");
            Console.WriteLine($"Etkinlik güncellenemedi: EventId={id}, StatusCode={response.StatusCode}");
            return new ResponseDto<EventDto> { IsSuccess = false, Message = "Etkinlik güncellenemedi" };
        }

        public async Task<ResponseDto<bool>> DeleteEventAsync(Guid id, Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints.Events}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                if (result != null)
                {
                    return result;
                }
            }
            
            return new ResponseDto<bool> { IsSuccess = false, Message = "Etkinlik silinemedi" };
        }

        public async Task<ResponseDto<EventStatisticsDto>> GetEventStatisticsAsync(Guid id, Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{id}/statistics");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<EventStatisticsDto>>(responseContent);
                
                if (result != null)
                {
                    return result;
                }
            }
            
            return new ResponseDto<EventStatisticsDto> { IsSuccess = false, Message = "Etkinlik istatistikleri alınamadı" };
        }

        public async Task<ResponseDto<List<EventDto>>> GetUpcomingEventsAsync(Guid tenantId)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
            }
            
            _httpClient.DefaultRequestHeaders.Add("X-Tenant", tenantId.ToString());
            _logger.LogInformation($"X-Tenant header'ı güncellendi: {tenantId}");
            
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/upcoming");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<EventDto>>>(responseContent);
                
                if (result != null)
                {
                    return result;
                }
            }
            
            return new ResponseDto<List<EventDto>> { IsSuccess = false, Message = "Yaklaşan etkinlikler alınamadı" };
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

        #region Attendee Methods
        public async Task<ResponseDto<List<AttendeeDto>>> GetAttendeesByEventIdAsync(Guid eventId)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Events}/{eventId}/attendees");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<AttendeeDto>>>(responseContent);
                
                return result ?? new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = "Veri alınamadı", Data = new List<AttendeeDto>() };
            }
            
            return new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = $"Katılımcılar alınamadı: {response.ReasonPhrase}", Data = new List<AttendeeDto>() };
        }

        public async Task<ResponseDto<AttendeeDto>> GetAttendeeByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Attendees}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<AttendeeDto>>(responseContent);
                
                return result ?? new ResponseDto<AttendeeDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<AttendeeDto> { IsSuccess = false, Message = $"Katılımcı alınamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<AttendeeDto>> CreateAttendeeAsync(CreateAttendeeDto createAttendeeDto)
        {
            var content = CreateJsonContent(createAttendeeDto);
            var response = await _httpClient.PostAsync($"{_apiSettings.Endpoints.Events}/{createAttendeeDto.EventId}/attendees", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<AttendeeDto>>(responseContent);
                
                return result ?? new ResponseDto<AttendeeDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<AttendeeDto> { IsSuccess = false, Message = $"Katılımcı oluşturulamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<AttendeeDto>> UpdateAttendeeAsync(Guid id, UpdateAttendeeDto updateAttendeeDto)
        {
            var content = CreateJsonContent(updateAttendeeDto);
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints.Attendees}/{id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<AttendeeDto>>(responseContent);
                
                return result ?? new ResponseDto<AttendeeDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<AttendeeDto> { IsSuccess = false, Message = $"Katılımcı güncellenemedi: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<bool>> DeleteAttendeeAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints.Attendees}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                return result ?? new ResponseDto<bool> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<bool> { IsSuccess = false, Message = $"Katılımcı silinemedi: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<AttendeeDto>> SearchAttendeeByEmailAsync(string email)
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Attendees}/search?email={Uri.EscapeDataString(email)}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<AttendeeDto>>(responseContent);
                
                return result ?? new ResponseDto<AttendeeDto> { IsSuccess = false, Message = "Veri alınamadı" };
            }
            
            return new ResponseDto<AttendeeDto> { IsSuccess = false, Message = $"Katılımcı bulunamadı: {response.ReasonPhrase}" };
        }

        public async Task<ResponseDto<List<AttendeeDto>>> SearchAttendeesAsync(SearchAttendeeDto searchDto)
        {
            var queryParams = new List<string>();
            
            if (searchDto.EventId.HasValue)
                queryParams.Add($"eventId={searchDto.EventId.Value}");
            
            if (!string.IsNullOrEmpty(searchDto.Name))
                queryParams.Add($"name={Uri.EscapeDataString(searchDto.Name)}");
            
            if (!string.IsNullOrEmpty(searchDto.Email))
                queryParams.Add($"email={Uri.EscapeDataString(searchDto.Email)}");
            
            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints.Attendees}/search{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<AttendeeDto>>>(responseContent);
                
                return result ?? new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = "Veri alınamadı", Data = new List<AttendeeDto>() };
            }
            
            return new ResponseDto<List<AttendeeDto>> { IsSuccess = false, Message = $"Katılımcılar bulunamadı: {response.ReasonPhrase}", Data = new List<AttendeeDto>() };
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

        public async Task<ResponseDto<TenantDto>> GetTenantBySubdomainAsync(string subdomain)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Tenants}/subdomain/{subdomain}";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var tenantResponse = JsonConvert.DeserializeObject<ResponseDto<TenantDto>>(responseContent);
                
                if (tenantResponse == null)
                {
                    _logger.LogError("Tenant yanıtı deserialize edilemedi: {Subdomain}", subdomain);
                    return new ResponseDto<TenantDto> 
                    { 
                        IsSuccess = false, 
                        Message = "Tenant yanıtı deserialize edilemedi" 
                    };
                }
                
                if (!tenantResponse.IsSuccess)
                {
                    _logger.LogWarning("Tenant bulunamadı: {Subdomain}, Hata: {Error}", subdomain, tenantResponse.Message);
                }
                
                return tenantResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant sorgulanırken hata oluştu: {Subdomain}", subdomain);
                return new ResponseDto<TenantDto> 
                { 
                    IsSuccess = false, 
                    Message = $"Tenant sorgulanırken hata oluştu: {ex.Message}" 
                };
            }
        }

        public async Task<ResponseDto<List<TenantDto>>> GetAllTenantsAsync()
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Tenants}";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var tenantsResponse = JsonConvert.DeserializeObject<ResponseDto<List<TenantDto>>>(responseContent);
                
                if (tenantsResponse == null)
                {
                    _logger.LogError("Tenant listesi yanıtı deserialize edilemedi");
                    return new ResponseDto<List<TenantDto>> 
                    { 
                        IsSuccess = false, 
                        Message = "Tenant listesi yanıtı deserialize edilemedi" 
                    };
                }
                
                if (!tenantsResponse.IsSuccess)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {Error}", tenantsResponse.Message);
                }
                
                return tenantsResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant listesi alınırken hata oluştu");
                return new ResponseDto<List<TenantDto>> 
                { 
                    IsSuccess = false, 
                    Message = $"Tenant listesi alınırken hata oluştu: {ex.Message}" 
                };
            }
        }
        #endregion

        #region User Methods
        public async Task<ResponseDto<List<UserDto>>> GetAllUsersAsync()
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var usersResponse = JsonConvert.DeserializeObject<ResponseDto<List<UserDto>>>(responseContent);
                
                if (usersResponse == null)
                {
                    return new ResponseDto<List<UserDto>> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return usersResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi alınırken hata oluştu");
                return new ResponseDto<List<UserDto>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<UserDto>> GetUserByIdAsync(Guid id)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/{id}";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var userResponse = JsonConvert.DeserializeObject<ResponseDto<UserDto>>(responseContent);
                
                if (userResponse == null)
                {
                    return new ResponseDto<UserDto> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return userResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgisi alınırken hata oluştu");
                return new ResponseDto<UserDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<UserDto>> CreateUserAsync(RegisterDto createUserDto)
        {
            try
            {
                // API isteği için kullanıcıyı kaydet 
                var registerResult = await RegisterAsync(createUserDto);
                
                if (!registerResult.IsSuccess)
                {
                    return new ResponseDto<UserDto> { IsSuccess = false, Message = registerResult.Message };
                }
                
                // Kullanıcı oluşturulduysa, yanıtı UserDto'ya dönüştür ve döndür
                var userDto = new UserDto
                {
                    Id = registerResult.UserId,
                    Email = createUserDto.Email,
                    Username = createUserDto.Username
                };
                
                return new ResponseDto<UserDto> { IsSuccess = true, Data = userDto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu");
                return new ResponseDto<UserDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/{id}";
                
                // İsteği oluştur
                var content = CreateJsonContent(updateUserDto);
                
                // İsteği gönder
                var response = await _httpClient.PutAsync(endpoint, content);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var userResponse = JsonConvert.DeserializeObject<ResponseDto<UserDto>>(responseContent);
                
                if (userResponse == null)
                {
                    return new ResponseDto<UserDto> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return userResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu");
                return new ResponseDto<UserDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/{id}";
                
                // İsteği gönder
                var response = await _httpClient.DeleteAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var deleteResponse = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                if (deleteResponse == null)
                {
                    return new ResponseDto<bool> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return deleteResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu");
                return new ResponseDto<bool> { IsSuccess = false, Message = ex.Message };
            }
        }
        #endregion

        #region Role Methods
        public async Task<ResponseDto<List<RoleDto>>> GetAllRolesAsync()
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/roles";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var rolesResponse = JsonConvert.DeserializeObject<ResponseDto<List<RoleDto>>>(responseContent);
                
                if (rolesResponse == null)
                {
                    return new ResponseDto<List<RoleDto>> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return rolesResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol listesi alınırken hata oluştu");
                return new ResponseDto<List<RoleDto>> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<RoleDto>> GetRoleByIdAsync(Guid id)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/roles/{id}";
                
                // İsteği gönder
                var response = await _httpClient.GetAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var roleResponse = JsonConvert.DeserializeObject<ResponseDto<RoleDto>>(responseContent);
                
                if (roleResponse == null)
                {
                    return new ResponseDto<RoleDto> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return roleResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol bilgisi alınırken hata oluştu");
                return new ResponseDto<RoleDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/roles";
                
                // İsteği oluştur
                var content = CreateJsonContent(createRoleDto);
                
                // İsteği gönder
                var response = await _httpClient.PostAsync(endpoint, content);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var roleResponse = JsonConvert.DeserializeObject<ResponseDto<RoleDto>>(responseContent);
                
                if (roleResponse == null)
                {
                    return new ResponseDto<RoleDto> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return roleResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol oluşturulurken hata oluştu");
                return new ResponseDto<RoleDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/roles/{id}";
                
                // İsteği oluştur
                var content = CreateJsonContent(updateRoleDto);
                
                // İsteği gönder
                var response = await _httpClient.PutAsync(endpoint, content);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var roleResponse = JsonConvert.DeserializeObject<ResponseDto<RoleDto>>(responseContent);
                
                if (roleResponse == null)
                {
                    return new ResponseDto<RoleDto> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return roleResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncellenirken hata oluştu");
                return new ResponseDto<RoleDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<bool>> DeleteRoleAsync(Guid id)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Users}/roles/{id}";
                
                // İsteği gönder
                var response = await _httpClient.DeleteAsync(endpoint);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var deleteResponse = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                if (deleteResponse == null)
                {
                    return new ResponseDto<bool> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return deleteResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol silinirken hata oluştu");
                return new ResponseDto<bool> { IsSuccess = false, Message = ex.Message };
            }
        }
        #endregion

        public async Task<ResponseDto<bool>> ApproveEventAsync(Guid id, Guid tenantId)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Events}/{id}/approve";
                
                // İsteği gönder
                var response = await _httpClient.PostAsync(endpoint, null);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var approveResponse = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                if (approveResponse == null)
                {
                    return new ResponseDto<bool> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return approveResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik onaylanırken hata oluştu");
                return new ResponseDto<bool> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ResponseDto<bool>> RejectEventAsync(Guid id, Guid tenantId)
        {
            try
            {
                // Endpoint URL'i oluştur
                var endpoint = $"{_apiSettings.Endpoints.Events}/{id}/reject";
                
                // İsteği gönder
                var response = await _httpClient.PostAsync(endpoint, null);
                
                // Yanıtı oku
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Yanıtı deserialize et
                var rejectResponse = JsonConvert.DeserializeObject<ResponseDto<bool>>(responseContent);
                
                if (rejectResponse == null)
                {
                    return new ResponseDto<bool> { IsSuccess = false, Message = "API yanıtı boş" };
                }
                
                return rejectResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Etkinlik reddedilirken hata oluştu");
                return new ResponseDto<bool> { IsSuccess = false, Message = ex.Message };
            }
        }

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
} 