using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Attendee
{
    public enum AttendeeStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2
    }

    public class AttendeeViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("eventId")]
        public Guid EventId { get; set; }
        
        [JsonPropertyName("name")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("phone")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        [Display(Name = "Durum")]
        public AttendeeStatus Status { get; set; }
        
        [JsonPropertyName("hasAttended")]
        [Display(Name = "Katıldı")]
        public bool HasAttended { get; set; }
        
        [JsonPropertyName("registrationDate")]
        [Display(Name = "Kayıt Tarihi")]
        public DateTime RegistrationDate { get; set; }
        
        [JsonPropertyName("notes")]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("isCancelled")]
        [Display(Name = "İptal Edildi")]
        public bool IsCancelled { get; set; }
        
        [JsonPropertyName("sendEmailNotification")]
        [Display(Name = "E-posta Bildirimi")]
        public bool SendEmailNotification { get; set; } = true;
    }
    
    public class CreateAttendeeViewModel
    {
        [Required(ErrorMessage = "Etkinlik ID'si gereklidir")]
        [JsonPropertyName("eventId")]
        public Guid EventId { get; set; }
        
        [Required(ErrorMessage = "Tenant ID'si gereklidir")]
        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }
        
        [Required(ErrorMessage = "Ad soyad alanı zorunludur")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir")]
        [Display(Name = "Ad Soyad")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "E-posta alanı zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
        [Display(Name = "E-posta")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Telefon alanı zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        [Display(Name = "Telefon")]
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;
        
        [Display(Name = "Notlar")]
        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir")]
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [Display(Name = "E-posta Bildirimi")]
        [JsonPropertyName("sendEmailNotification")]
        public bool SendEmailNotification { get; set; } = true;
    }
} 