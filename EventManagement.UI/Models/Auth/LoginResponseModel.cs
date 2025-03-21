using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Auth
{
    public class LoginResponseModel
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }
    }
} 