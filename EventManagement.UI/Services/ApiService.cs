using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace EventManagement.UI.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:2025/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        // IApiService arayüz implementasyonu - Simple metotlar
        public async Task<HttpResponseMessage> GetAsync(string endpoint, string? token = null)
        {
            // HttpResponseAsync metoduna yönlendir
            return await GetHttpResponseAsync(endpoint, token);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data, string? token = null)
        {
            // HttpResponseAsync metoduna yönlendir
            return await PostHttpResponseAsync(endpoint, data, token);
        }

        public async Task<HttpResponseMessage> PutAsync(string endpoint, object data, string? token = null)
        {
            // HttpResponseAsync metoduna yönlendir
            return await PutHttpResponseAsync(endpoint, data, token);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint, string? token = null)
        {
            // HttpResponseAsync metoduna yönlendir
            return await DeleteHttpResponseAsync(endpoint, token);
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, string? token = null)
        {
            // WithHeaders metoduna yönlendir (boş headers ile)
            return await GetAsyncWithHeaders<T>(endpoint, token, null);
        }

        public async Task<ApiResponse<T>> GetAsyncWithHeaders<T>(string endpoint, string? token = null, Dictionary<string, string>? customHeaders = null)
        {
            try
            {
                PrepareAuthenticationHeader(token);

                // Özel headerları ekle
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        // Eğer header zaten varsa, önce kaldır
                        if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        {
                            _httpClient.DefaultRequestHeaders.Remove(header.Key);
                        }
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        _logger.LogInformation("Özel header eklendi: {Key}={Value}", header.Key, header.Value);
                    }
                }
                
                _logger.LogInformation("API İstek URL: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
                _logger.LogInformation("API İstek Hedef Tip: {Type}", typeof(T).FullName);
                
                var response = await _httpClient.GetAsync(endpoint);
                
                _logger.LogInformation("API Yanıt StatusCode: {StatusCode}", response.StatusCode);
                
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Çağrı Hatası: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"API çağrısı sırasında hata oluştu: {ex.Message}",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, string? token = null)
        {
            // WithHeaders metoduna yönlendir (boş headers ile)
            return await PostAsyncWithHeaders<T>(endpoint, data, token, null);
        }

        public async Task<ApiResponse<T>> PostAsyncWithHeaders<T>(string endpoint, object data, string? token = null, Dictionary<string, string>? customHeaders = null)
        {
            try
            {
                PrepareAuthenticationHeader(token);
                
                // Özel headerları ekle
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        // Eğer header zaten varsa, önce kaldır
                        if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        {
                            _httpClient.DefaultRequestHeaders.Remove(header.Key);
                        }
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        _logger.LogInformation("Özel header eklendi: {Key}={Value}", header.Key, header.Value);
                    }
                }
                
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _logger.LogInformation("API İstek: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
                _logger.LogInformation("İçerik: {Json}", json);
                
                var response = await _httpClient.PostAsync(endpoint, content);
                _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);
                
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Hatası: {Message}", ex.Message);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"API çağrısı sırasında hata oluştu: {ex.Message}",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data, string? token = null)
        {
            // WithHeaders metoduna yönlendir (boş headers ile)
            return await PutAsyncWithHeaders<T>(endpoint, data, token, null);
        }

        public async Task<ApiResponse<T>> PutAsyncWithHeaders<T>(string endpoint, object data, string? token = null, Dictionary<string, string>? customHeaders = null)
        {
            try
            {
                PrepareAuthenticationHeader(token);
                
                // Özel headerları ekle
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        // Eğer header zaten varsa, önce kaldır
                        if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        {
                            _httpClient.DefaultRequestHeaders.Remove(header.Key);
                        }
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        _logger.LogInformation("Özel header eklendi: {Key}={Value}", header.Key, header.Value);
                    }
                }
                
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                _logger.LogInformation("API İstek: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
                _logger.LogInformation("İçerik: {Json}", json);
                
                var response = await _httpClient.PutAsync(endpoint, content);
                _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);
                
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Hatası: {Message}", ex.Message);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"API çağrısı sırasında hata oluştu: {ex.Message}",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, string? token = null)
        {
            // WithHeaders metoduna yönlendir (boş headers ile)
            return await DeleteAsyncWithHeaders<T>(endpoint, token, null);
        }

        public async Task<ApiResponse<T>> DeleteAsyncWithHeaders<T>(string endpoint, string? token = null, Dictionary<string, string>? customHeaders = null)
        {
            try
            {
                PrepareAuthenticationHeader(token);
                
                // Özel headerları ekle
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        // Eğer header zaten varsa, önce kaldır
                        if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        {
                            _httpClient.DefaultRequestHeaders.Remove(header.Key);
                        }
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        _logger.LogInformation("Özel header eklendi: {Key}={Value}", header.Key, header.Value);
                    }
                }
                
                _logger.LogInformation("API İstek URL: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
                var response = await _httpClient.DeleteAsync(endpoint);
                _logger.LogInformation("Yanıt Kodu: {StatusCode}", response.StatusCode);
                
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Hatası: {Message}", ex.Message);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"API çağrısı sırasında hata oluştu: {ex.Message}",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        private void PrepareAuthenticationHeader(string? token)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // API isteklerinde subdomain'i header olarak ekleme
            var subdomain = _configuration["ApiSettings:DefaultTenant"];
            if (!string.IsNullOrEmpty(subdomain))
            {
                if (_httpClient.DefaultRequestHeaders.Contains("X-Tenant"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Tenant");
                }
                _httpClient.DefaultRequestHeaders.Add("X-Tenant", subdomain);
                _logger.LogInformation("API isteğine X-Tenant eklenmiştir: {Tenant}", subdomain);
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("API isteğine Authorization token eklenmiştir");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _logger.LogWarning("API isteğine Authorization token eklenmemiştir");
            }
        }

        private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();
                
            _logger.LogInformation("API yanıtı alındı: StatusCode={StatusCode}, ContentLength={ContentLength}", 
                statusCode, content?.Length ?? 0);
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("API yanıtı boş veya null!");
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "API yanıtı boş veya null!",
                    StatusCode = statusCode
                };
            }
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    _logger.LogInformation("Başarılı yanıt deserialize ediliyor...");
                    
                    // Önce içeriği logla
                    _logger.LogDebug("API Yanıt İçeriği: {Content}", content);
                    
                    // Yanıt wrapper içinde mi kontrol et
                    if (content.Contains("\"isSuccess\":") || content.Contains("\"data\":"))
                    {
                        var wrapper = JsonConvert.DeserializeObject<ApiWrapper<T>>(content);
                        
                        if (wrapper != null)
                        {
                            return new ApiResponse<T>
                            {
                                Success = wrapper.IsSuccess,
                                Message = wrapper.Message ?? "İşlem başarılı",
                                Data = wrapper.Data,
                                Errors = wrapper.Errors,
                                StatusCode = statusCode
                            };
                        }
                    }
                    
                    // Wrapper olmadan doğrudan veri içeriyor olabilir
                    var data = JsonConvert.DeserializeObject<T>(content);
                    
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Message = "İşlem başarılı",
                        Data = data,
                        StatusCode = statusCode
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Yanıt deserialize edilirken hata oluştu: {Message}, İçerik: {Content}", 
                        ex.Message, content);
                    
                    return new ApiResponse<T>
                    {
                        Success = false,
                        Message = $"Yanıt işlenirken hata oluştu: {ex.Message}",
                        StatusCode = statusCode
                    };
                }
            }
            else
            {
                var errorMessage = "Bir hata oluştu";
                
                try
                {
                    _logger.LogWarning("Hata yanıtı deserialize edilmeye çalışılıyor... İçerik: {Content}", content);
                    
                    // İçerik JSON mu kontrol et
                    if (content.StartsWith("{") && content.EndsWith("}"))
                    {
                        try
                        {
                            // Wrapper formatındaysa
                            var wrapper = JsonConvert.DeserializeObject<ApiWrapper<T>>(content);
                            if (wrapper != null && !string.IsNullOrEmpty(wrapper.Message))
                            {
                                return new ApiResponse<T>
                                {
                                    Success = false,
                                    Message = wrapper.Message,
                                    Errors = wrapper.Errors,
                                    StatusCode = statusCode
                                };
                            }
                        }
                        catch
                        {
                            // Wrapper formatında değilse, generic dictionary olarak parse et
                            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                            
                            if (json != null)
                            {
                                if (json.TryGetValue("message", out var message) && message != null)
                                {
                                    errorMessage = message.ToString();
                                }
                                else if (json.TryGetValue("Message", out var messageCapital) && messageCapital != null)
                                {
                                    errorMessage = messageCapital.ToString();
                                }
                                else if (json.TryGetValue("error", out var error) && error != null)
                                {
                                    errorMessage = error.ToString();
                                }
                                else if (json.TryGetValue("Error", out var errorCapital) && errorCapital != null)
                                {
                                    errorMessage = errorCapital.ToString();
                                }
                                
                                _logger.LogWarning("Çıkarılan hata mesajı: {ErrorMessage}", errorMessage);
                            }
                        }
                    }
                    else
                    {
                        // JSON değilse, muhtemelen HTML veya başka bir format
                        errorMessage = $"API hata döndürdü (HTTP {statusCode}): {(content.Length > 100 ? content.Substring(0, 100) + "..." : content)}";
                        _logger.LogWarning("API JSON olmayan bir yanıt döndürdü: {ErrorMessage}", errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata Yanıtı İşleme Hatası: {Message}", ex.Message);
                    errorMessage = $"API hatası (HTTP {statusCode}): {ex.Message}";
                }

                return new ApiResponse<T>
                {
                    Success = false,
                    Message = errorMessage,
                    StatusCode = statusCode
                };
            }
        }

        // API'den gelen yanıt wrapper sınıfı
        private class ApiWrapper<T>
        {
            [JsonProperty("isSuccess")]
            public bool IsSuccess { get; set; }
            
            [JsonProperty("message")]
            public string? Message { get; set; }
            
            [JsonProperty("data")]
            public T? Data { get; set; }
            
            [JsonProperty("errors")]
            public object? Errors { get; set; }
        }

        // HttpResponseMessage döndüren metotlar
        public async Task<HttpResponseMessage> GetHttpResponseAsync(string endpoint, string? token = null)
        {
            PrepareAuthenticationHeader(token);
            _logger.LogInformation("API İstek URL: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
            return await _httpClient.GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PostHttpResponseAsync(string endpoint, object data, string? token = null)
        {
            PrepareAuthenticationHeader(token);
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("API İstek: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
            _logger.LogInformation("İçerik: {Json}", json);
            return await _httpClient.PostAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> PutHttpResponseAsync(string endpoint, object data, string? token = null)
        {
            PrepareAuthenticationHeader(token);
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("API İstek: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
            _logger.LogInformation("İçerik: {Json}", json);
            return await _httpClient.PutAsync(endpoint, content);
        }

        public async Task<HttpResponseMessage> DeleteHttpResponseAsync(string endpoint, string? token = null)
        {
            PrepareAuthenticationHeader(token);
            _logger.LogInformation("API İstek URL: {BaseUrl}{Endpoint}", _baseUrl, endpoint);
            return await _httpClient.DeleteAsync(endpoint);
        }
    }
} 