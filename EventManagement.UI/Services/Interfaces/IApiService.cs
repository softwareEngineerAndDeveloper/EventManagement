using EventManagement.UI.Models.Shared;
using System.Net.Http;

namespace EventManagement.UI.Services.Interfaces
{
    public interface IApiService
    {
        Task<ApiResponse<T>> GetAsync<T>(string endpoint, string? token = null);
        Task<ApiResponse<T>> GetAsyncWithHeaders<T>(string endpoint, string? token = null, Dictionary<string, string>? customHeaders = null);
        Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data, string? token = null);
        Task<ApiResponse<T>> PostAsyncWithHeaders<T>(string endpoint, object data, string? token = null, Dictionary<string, string>? customHeaders = null);
        Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data, string? token = null);
        Task<ApiResponse<T>> PutAsyncWithHeaders<T>(string endpoint, object data, string? token = null, Dictionary<string, string>? customHeaders = null);
        Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, string? token = null);
        Task<ApiResponse<T>> DeleteAsyncWithHeaders<T>(string endpoint, string? token = null, Dictionary<string, string>? customHeaders = null);
        
        Task<HttpResponseMessage> GetHttpResponseAsync(string endpoint, string? token = null);
        Task<HttpResponseMessage> PostHttpResponseAsync(string endpoint, object data, string? token = null);
        Task<HttpResponseMessage> PutHttpResponseAsync(string endpoint, object data, string? token = null);
        Task<HttpResponseMessage> DeleteHttpResponseAsync(string endpoint, string? token = null);
    }
} 