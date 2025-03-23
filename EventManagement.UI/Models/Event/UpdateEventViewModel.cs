using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventManagement.UI.Models.Event
{
    public class UpdateEventViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
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
        
        [Display(Name = "Toplam Katılımcı")]
        [Range(1, 10000, ErrorMessage = "Katılımcı sayısı 1 ile 10000 arasında olmalıdır")]
        [JsonPropertyName("maxAttendees")]
        public int? MaxAttendees { get; set; }
        
        [Display(Name = "Halka Açık")]
        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; } = true;
        
        [Display(Name = "İptal Edildi")]
        [JsonPropertyName("isCancelled")]
        public bool IsCancelled { get; set; } = false;
        
        [Display(Name = "Durum")]
        [JsonPropertyName("status")]
        public EventStatus Status { get; set; } = EventStatus.Pending;
    }
} 