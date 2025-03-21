using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Event
{
    public enum EventStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class EventViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Etkinlik başlığı gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Etkinlik açıklaması gereklidir")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Konum bilgisi gereklidir")]
        [StringLength(200, ErrorMessage = "Konum en fazla 200 karakter olabilir")]
        [Display(Name = "Konum")]
        public string Location { get; set; } = string.Empty;
        
        [Display(Name = "Toplam Katılımcı")]
        public int? MaxAttendees { get; set; }
        
        [Display(Name = "Kapasite")]
        public int? Capacity { get; set; }
        
        [Display(Name = "Halka Açık")]
        public bool IsPublic { get; set; } = true;
        
        [Display(Name = "İptal Edildi")]
        public bool IsCancelled { get; set; } = false;
        
        [Display(Name = "Durum")]
        public EventStatus Status { get; set; } = EventStatus.Pending;
        
        [Display(Name = "Oluşturan")]
        public string CreatorName { get; set; } = string.Empty;
        
        [Display(Name = "Oluşturan ID")]
        public Guid CreatorId { get; set; }
        
        [Display(Name = "Kayıt Sayısı")]
        public int RegistrationCount { get; set; }
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
    }
} 