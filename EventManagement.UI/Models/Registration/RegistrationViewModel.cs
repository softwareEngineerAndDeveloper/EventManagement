using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Registration
{
    public enum RegistrationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Attended,
        WaitingList
    }

    public class RegistrationViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Etkinlik ID")]
        public Guid EventId { get; set; }
        
        [Display(Name = "Etkinlik Adı")]
        public string EventTitle { get; set; } = string.Empty;
        
        [Display(Name = "Katılımcı ID")]
        public Guid AttendeeId { get; set; }
        
        [Display(Name = "Katılımcı Adı")]
        public string AttendeeName { get; set; } = string.Empty;
        
        [Display(Name = "Katılımcı E-posta")]
        public string AttendeeEmail { get; set; } = string.Empty;
        
        [Display(Name = "Kayıt Durumu")]
        public RegistrationStatus Status { get; set; }
        
        [Display(Name = "Kayıt Tarihi")]
        public DateTime RegistrationDate { get; set; }
        
        [Display(Name = "Onay Tarihi")]
        public DateTime? ConfirmationDate { get; set; }
        
        [Display(Name = "Katılım Tarihi")]
        public DateTime? AttendanceDate { get; set; }

        [Display(Name = "Not")]
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir")]
        public string? Notes { get; set; }
    }
} 