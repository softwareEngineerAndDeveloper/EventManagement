using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services.Interfaces;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace EventManagement.UI.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:2025/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, string? token = null)
        {
            try
            {
                PrepareAuthenticationHeader(token);
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
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
            try
            {
                PrepareAuthenticationHeader(token);
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                Console.WriteLine($"API İstek: {_baseUrl}{endpoint}");
                Console.WriteLine($"İçerik: {json}");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                Console.WriteLine($"Yanıt Kodu: {response.StatusCode}");
                
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
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
            try
            {
                PrepareAuthenticationHeader(token);
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
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
            try
            {
                PrepareAuthenticationHeader(token);
                var response = await _httpClient.DeleteAsync(endpoint);
                return await ProcessResponseAsync<T>(response);
            }
            catch (Exception ex)
            {
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
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            Console.WriteLine($"API Yanıt İçeriği: {content}");
            Console.WriteLine($"API Yanıt Durum Kodu: {statusCode}");
            Console.WriteLine($"API Yanıt Hedef Tip: {typeof(T).FullName}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // İçerik boş kontrolü
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        Console.WriteLine("API yanıtı boş içerik döndürdü!");
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = "API yanıtı boş içerik döndürdü",
                            StatusCode = statusCode
                        };
                    }

                    // JSON formatını doğrula
                    try
                    {
                        var testParse = JsonConvert.DeserializeObject(content);
                        Console.WriteLine($"JSON formatı geçerli: {testParse != null}");
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"JSON formatı geçersiz: {jsonEx.Message}");
                    }

                    // API yanıtı wrapper içinde geliyor, önce wrapper'ı deserialize ediyoruz
                    Console.WriteLine("API wrapper deserialize deneniyor...");
                    var apiWrapper = JsonConvert.DeserializeObject<ApiWrapper<T>>(content);
                    
                    if (apiWrapper != null)
                    {
                        Console.WriteLine($"API wrapper deserialize başarılı. IsSuccess: {apiWrapper.IsSuccess}");
                        return new ApiResponse<T>
                        {
                            Success = apiWrapper.IsSuccess,
                            Message = apiWrapper.Message ?? "İşlem başarılı",
                            Data = apiWrapper.Data,
                            StatusCode = statusCode
                        };
                    }
                    
                    // Eğer wrapper yoksa, doğrudan içeriği deserialize et
                    Console.WriteLine("Doğrudan tip deserialize deneniyor...");
                    var data = JsonConvert.DeserializeObject<T>(content);
                    Console.WriteLine($"Doğrudan deserialize sonucu: {data != null}");
                    
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
                    Console.WriteLine($"JSON Dönüştürme Hatası: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    
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
                    Console.WriteLine("Hata yanıtı deserialize ediliyor...");
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
                    Console.WriteLine($"Hata yanıtı: {errorResponse}");
                    
                    if (errorResponse?.message != null)
                    {
                        errorMessage = errorResponse.message.ToString();
                        Console.WriteLine($"Çıkarılan hata mesajı: {errorMessage}");
                    }
                    else if (errorResponse?.Message != null)
                    {
                        errorMessage = errorResponse.Message.ToString();
                        Console.WriteLine($"Çıkarılan hata mesajı (Message): {errorMessage}");
                    }
                    else if (errorResponse?.error != null)
                    {
                        errorMessage = errorResponse.error.ToString();
                        Console.WriteLine($"Çıkarılan hata mesajı (error): {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata Yanıtı İşleme Hatası: {ex.Message}");
                    // JSON deserialize hatası durumunda ham içeriği kullan
                    errorMessage = !string.IsNullOrEmpty(content) ? content : $"HTTP hata kodu: {statusCode}";
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
    }
} 