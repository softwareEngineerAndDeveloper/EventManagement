using System.ComponentModel.DataAnnotations;
using EventManagement.Domain.Entities;

namespace EventManagement.UI.DTOs
{
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

        [Display(Name = "Maksimum Katılımcı")]
        [Range(1, int.MaxValue, ErrorMessage = "Maksimum katılımcı sayısı en az {1} olmalıdır")]
        public int? MaxAttendees { get; set; }

        [Display(Name = "Halka Açık")]
        public bool IsPublic { get; set; } = true;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Required]
        public Guid CreatorId { get; set; }
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