using EventManagement.UI.Models;
using EventManagement.UI.Models.Role;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EventManagement.UI.Services
{
    public class RoleService : EventManagement.UI.Services.Interfaces.IRoleService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoleService> _logger;
        private readonly string _apiBaseUrl;

        public RoleService(HttpClient httpClient, IConfiguration configuration, ILogger<RoleService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"]?.TrimEnd('/');
            
            _logger.LogInformation("RoleService oluşturuldu. API Base URL: {BaseUrl}", _apiBaseUrl);
        }

        public async Task<ResponseModel<List<RoleViewModel>>> GetAllRolesAsync(string token)
        {
            try
            {
                string apiUrl = $"{_apiBaseUrl}/api/roles";
                _logger.LogInformation("Roller için API çağrısı yapılıyor. URL: {ApiUrl}", apiUrl);
                
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync(apiUrl);
                
                _logger.LogInformation("API yanıtı alındı. Status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API yanıt içeriği: {Content}", content);
                    
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<RoleViewModel>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse != null && apiResponse.IsSuccess)
                    {
                        return ResponseModel<List<RoleViewModel>>.Success(apiResponse.Data);
                    }
                    
                    return ResponseModel<List<RoleViewModel>>.Fail(apiResponse?.Message ?? "Roller alınırken bir hata oluştu");
                }
                
                _logger.LogWarning("API başarısız yanıt döndü. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                    
                return ResponseModel<List<RoleViewModel>>.Fail($"Roller alınırken bir hata oluştu. HTTP Durum Kodu: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roller alınırken bir hata oluştu");
                return ResponseModel<List<RoleViewModel>>.Fail($"Roller alınırken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<RoleViewModel>> GetRoleByIdAsync(Guid id, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/roles/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<RoleViewModel>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse != null && apiResponse.IsSuccess)
                    {
                        return ResponseModel<RoleViewModel>.Success(apiResponse.Data);
                    }
                    
                    return ResponseModel<RoleViewModel>.Fail(apiResponse?.Message ?? "Rol bilgileri alınırken bir hata oluştu");
                }
                
                return ResponseModel<RoleViewModel>.Fail($"Rol bilgileri alınırken bir hata oluştu. HTTP Durum Kodu: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol bilgileri alınırken bir hata oluştu. Id: {Id}", id);
                return ResponseModel<RoleViewModel>.Fail($"Rol bilgileri alınırken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<RoleViewModel>> CreateRoleAsync(CreateRoleViewModel model, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/roles", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<RoleViewModel>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse != null && apiResponse.IsSuccess)
                    {
                        return ResponseModel<RoleViewModel>.Success(apiResponse.Data);
                    }
                    
                    return ResponseModel<RoleViewModel>.Fail(apiResponse?.Message ?? "Rol oluşturulurken bir hata oluştu");
                }
                
                return ResponseModel<RoleViewModel>.Fail($"Rol oluşturulurken bir hata oluştu. HTTP Durum Kodu: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol oluşturulurken bir hata oluştu");
                return ResponseModel<RoleViewModel>.Fail($"Rol oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<RoleViewModel>> UpdateRoleAsync(UpdateRoleViewModel model, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                string apiUrl = $"{_apiBaseUrl}/api/roles/{model.Id}";
                _logger.LogInformation("Rol güncelleniyor. URL: {ApiUrl}, RoleId: {RoleId}, RoleName: {RoleName}",
                    apiUrl, model.Id, model.Name);
                
                var json = JsonSerializer.Serialize(model);
                _logger.LogInformation("API'ye gönderilen veri: {JsonData}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(apiUrl, content);
                _logger.LogInformation("API yanıtı alındı. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API başarılı yanıt içeriği: {Content}", responseContent);
                    
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<RoleViewModel>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse != null && apiResponse.IsSuccess)
                    {
                        return ResponseModel<RoleViewModel>.Success(apiResponse.Data);
                    }
                    
                    _logger.LogWarning("API yanıtı başarılı kod döndü ancak içerikte hata var: {Message}", 
                        apiResponse?.Message ?? "Bilinmeyen hata");
                    
                    return ResponseModel<RoleViewModel>.Fail(apiResponse?.Message ?? "Rol güncellenirken bir hata oluştu");
                }
                
                string errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API başarısız yanıt içeriği: {ErrorContent}", errorContent);
                
                return ResponseModel<RoleViewModel>.Fail($"Rol güncellenirken bir hata oluştu. HTTP Durum Kodu: {response.StatusCode}, Mesaj: {errorContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncellenirken bir hata oluştu. Id: {Id}, Name: {Name}", model.Id, model.Name);
                return ResponseModel<RoleViewModel>.Fail($"Rol güncellenirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<bool>> DeleteRoleAsync(Guid id, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/roles/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse != null && apiResponse.IsSuccess)
                    {
                        return ResponseModel<bool>.Success(apiResponse.Data);
                    }
                    
                    return ResponseModel<bool>.Fail(apiResponse?.Message ?? "Rol silinirken bir hata oluştu");
                }
                
                return ResponseModel<bool>.Fail($"Rol silinirken bir hata oluştu. HTTP Durum Kodu: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol silinirken bir hata oluştu. Id: {Id}", id);
                return ResponseModel<bool>.Fail($"Rol silinirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ResponseModel<bool>> TestConnectionAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                string apiUrl = $"{_apiBaseUrl}/api/roles";
                _logger.LogInformation("Bağlantı testi için API çağrısı yapılıyor. URL: {ApiUrl}", apiUrl);
                
                // Test amaçlı bağlantı sınaması
                var response = await _httpClient.GetAsync(apiUrl);
                
                _logger.LogInformation("Bağlantı testi yanıtı. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                
                if (response.IsSuccessStatusCode)
                {
                    return ResponseModel<bool>.Success(true, "Bağlantı başarılı");
                }
                
                return ResponseModel<bool>.Fail($"API bağlantı hatası: HTTP Durum Kodu: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API bağlantı testi sırasında hata oluştu");
                return ResponseModel<bool>.Fail($"API bağlantı hatası: {ex.Message}");
            }
        }
    }
} 