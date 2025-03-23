using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Event
{
    public class EventCreateViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Etkinlik başlığı gereklidir.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Açıklama gereklidir.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Konum gereklidir.")]
        [Display(Name = "Konum")]
        public string Location { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Maksimum katılımcı sayısı gereklidir.")]
        [Display(Name = "Maksimum Katılımcı Sayısı")]
        [Range(1, int.MaxValue, ErrorMessage = "Maksimum katılımcı sayısı en az 1 olmalıdır.")]
        public int MaxAttendees { get; set; } = 100;
        
        [Display(Name = "Kapasite")]
        [Range(1, int.MaxValue, ErrorMessage = "Kapasite en az 1 olmalıdır.")]
        public int Capacity { get; set; } = 100;
        
        [Display(Name = "Herkese Açık")]
        public bool IsPublic { get; set; } = true;
        
        [Display(Name = "Kiracı")]
        public Guid? TenantId { get; set; }
    }
} 