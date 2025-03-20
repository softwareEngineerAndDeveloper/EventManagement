using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.DTOs
{
    public enum EventStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public bool IsActive { get; set; }
        public Guid TenantId { get; set; }
        public int RegistrationCount { get; set; }
        public EventStatus Status { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsPublic { get; set; }
        public int? MaxAttendees { get; set; }
        public Guid? CreatorId { get; set; }
    }

    public class CreateEventDto
    {
        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla {1} karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; } = DateTime.Now.Date.AddDays(1);

        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; } = DateTime.Now.Date.AddDays(1).AddHours(1);

        [Required(ErrorMessage = "Konum gereklidir")]
        [StringLength(200, ErrorMessage = "Konum en fazla {1} karakter olabilir")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Kapasite")]
        [Range(1, int.MaxValue, ErrorMessage = "Kapasite en az {1} olmalıdır")]
        public int? Capacity { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class UpdateEventDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Başlık gereklidir")]
        [StringLength(100, ErrorMessage = "Başlık en fazla {1} karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Konum gereklidir")]
        [StringLength(200, ErrorMessage = "Konum en fazla {1} karakter olabilir")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Kapasite")]
        [Range(1, int.MaxValue, ErrorMessage = "Kapasite en az {1} olmalıdır")]
        public int? Capacity { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Maksimum Katılımcı")]
        public int? MaxAttendees { get; set; }
        
        [Display(Name = "Halka Açık")]
        public bool IsPublic { get; set; }
        
        [Display(Name = "İptal Edildi")]
        public bool IsCancelled { get; set; }
        
        [Display(Name = "Durum")]
        public EventStatus Status { get; set; }
    }

    public class EventStatisticsDto
    {
        public int TotalRegistrations { get; set; }
        public int ConfirmedRegistrations { get; set; }
        public int CancelledRegistrations { get; set; }
        public int WaitingListRegistrations { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    public class EventFiltersDto
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ResponseDto<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
} 