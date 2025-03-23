using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.User
{
    public class UserViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("firstName")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }

        [JsonPropertyName("tenantName")]
        [Display(Name = "Kiracı")]
        public string TenantName { get; set; } = string.Empty;

        [JsonPropertyName("roles")]
        [Display(Name = "Roller")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("isActive")]
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("isVerified")]
        [Display(Name = "Doğrulanmış")]
        public bool IsVerified { get; set; } = false;

        [JsonPropertyName("createdAt")]
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("lastLoginAt")]
        [Display(Name = "Son Giriş Tarihi")]
        public DateTime? LastLoginAt { get; set; }

        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
    
    public class UpdateUserViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Ad gereklidir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Soyad gereklidir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }
        
        [Display(Name = "Telefon")]
        public string Phone { get; set; }
        
        [Display(Name = "Durum")]
        public bool IsActive { get; set; }
    }
} 