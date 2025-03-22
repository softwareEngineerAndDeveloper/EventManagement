using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Shared
{
    public class ApiWrapper<T>
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        
        [JsonPropertyName("errors")]
        public Dictionary<string, List<string>>? Errors { get; set; }
    }
} 