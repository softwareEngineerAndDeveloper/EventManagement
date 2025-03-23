using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Event
{
    public enum EventStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Active,
        Draft,
        Planning,
        Preparation,
        Completed
    }

    public class EventViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }
        
        [Required(ErrorMessage = "Etkinlik başlığı gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        [Display(Name = "Başlık")]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Etkinlik açıklaması gereklidir")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        [Display(Name = "Açıklama")]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [Display(Name = "Başlangıç Tarihi")]
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        [Display(Name = "Bitiş Tarihi")]
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Konum bilgisi gereklidir")]
        [StringLength(200, ErrorMessage = "Konum en fazla 200 karakter olabilir")]
        [Display(Name = "Konum")]
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;
        
        [Display(Name = "Maksimum Katılımcı Sayısı")]
        [JsonPropertyName("maxAttendees")]
        public int MaxAttendees { get; set; }
        
        [Display(Name = "Katılımcı Sayısı")]
        [JsonPropertyName("attendeeCount")]
        public int AttendeeCount { get; set; }
        
        [Display(Name = "Kayıt Sayısı")]
        [JsonPropertyName("registrationCount")]
        public int RegistrationCount { get; set; }
        
        [Display(Name = "Kapasite")]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
        
        [Display(Name = "Herkese Açık")]
        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; } = true;
        
        [Display(Name = "İptal Edildi")]
        [JsonPropertyName("isCancelled")]
        public bool IsCancelled { get; set; } = false;
        
        [Display(Name = "Durum")]
        [JsonPropertyName("status")]
        public EventStatus Status { get; set; } = EventStatus.Pending;
        
        [Display(Name = "Oluşturulma Tarihi")]
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Oluşturan")]
        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
        
        [Display(Name = "Oluşturan ID")]
        [JsonPropertyName("creatorId")]
        public string CreatorId { get; set; } = string.Empty;
        
        [Display(Name = "Kiracı Adı")]
        [JsonPropertyName("tenantName")]
        public string TenantName { get; set; } = string.Empty;
        
        [JsonIgnore]
        [Display(Name = "Tenant Subdomain")]
        public string TenantSubdomain { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string StatusClass { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string StatusText { get; set; } = string.Empty;
    }
} 