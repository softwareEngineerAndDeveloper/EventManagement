using EventManagement.UI.Models;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.User;
using EventManagement.UI.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EventManagement.UI.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserService> _logger;
        private readonly string _baseUrl;

        public UserService(HttpClient httpClient, IConfiguration configuration, ILogger<UserService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("ApiSettings:BaseUrl");
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<ResponseModel<List<UserViewModel>>> GetAllUsersAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync("api/users");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserViewModel>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return new ResponseModel<List<UserViewModel>>
                    {
                        IsSuccess = true,
                        Data = users ?? new List<UserViewModel>(),
                        Message = "Kullanıcılar başarıyla alındı."
                    };
                }
                
                return new ResponseModel<List<UserViewModel>>
                {
                    IsSuccess = false,
                    Data = new List<UserViewModel>(),
                    Message = $"Kullanıcılar alınırken bir hata oluştu. Status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar alınırken bir hata oluştu");
                return new ResponseModel<List<UserViewModel>>
                {
                    IsSuccess = false,
                    Data = new List<UserViewModel>(),
                    Message = $"Kullanıcılar alınırken bir hata oluştu: {ex.Message}"
                };
            }
        }

        public async Task<ResponseModel<UserViewModel>> GetUserByIdAsync(Guid id, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync($"api/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserViewModel>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return new ResponseModel<UserViewModel>
                    {
                        IsSuccess = true,
                        Data = user ?? new UserViewModel(),
                        Message = "Kullanıcı başarıyla alındı."
                    };
                }
                
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı alınırken bir hata oluştu. Status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgileri alınırken bir hata oluştu. Id: {Id}", id);
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı alınırken bir hata oluştu: {ex.Message}"
                };
            }
        }

        public async Task<ResponseModel<UserViewModel>> GetCurrentUserAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync("api/users/current");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserViewModel>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return new ResponseModel<UserViewModel>
                    {
                        IsSuccess = true,
                        Data = user ?? new UserViewModel(),
                        Message = "Kullanıcı bilgileri başarıyla alındı."
                    };
                }
                
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı bilgileri alınırken bir hata oluştu. Status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mevcut kullanıcı bilgileri alınırken bir hata oluştu");
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı bilgileri alınırken bir hata oluştu: {ex.Message}"
                };
            }
        }

        public async Task<ResponseModel<UserViewModel>> UpdateUserAsync(UpdateUserViewModel model, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"api/users/{model.Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserViewModel>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return new ResponseModel<UserViewModel>
                    {
                        IsSuccess = true,
                        Data = user ?? new UserViewModel(),
                        Message = "Kullanıcı başarıyla güncellendi."
                    };
                }
                
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı güncellenirken bir hata oluştu. Status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken bir hata oluştu. Id: {Id}", model.Id);
                return new ResponseModel<UserViewModel>
                {
                    IsSuccess = false,
                    Data = new UserViewModel(),
                    Message = $"Kullanıcı güncellenirken bir hata oluştu: {ex.Message}"
                };
            }
        }

        public async Task<ResponseModel<bool>> DeleteUserAsync(Guid id, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.DeleteAsync($"api/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return new ResponseModel<bool>
                    {
                        IsSuccess = true,
                        Data = true,
                        Message = "Kullanıcı başarıyla silindi."
                    };
                }
                
                return new ResponseModel<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    Message = $"Kullanıcı silinirken bir hata oluştu. Status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken bir hata oluştu. Id: {Id}", id);
                return new ResponseModel<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    Message = $"Kullanıcı silinirken bir hata oluştu: {ex.Message}"
                };
            }
        }
    }
} 