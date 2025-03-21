using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Attendee
{
    public class AttendeeViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Ad alanı gereklidir")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Soyad alanı gereklidir")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;
        
        [Display(Name = "Tam Ad")]
        public string FullName => $"{FirstName} {LastName}";
        
        [Display(Name = "Şirket")]
        public string? Company { get; set; }
        
        [Display(Name = "Unvan")]
        public string? JobTitle { get; set; }
        
        [Display(Name = "Adres")]
        public string? Address { get; set; }
        
        [Display(Name = "Şehir")]
        public string? City { get; set; }
        
        [Display(Name = "Ülke")]
        public string? Country { get; set; }
        
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        [Display(Name = "Etkinlik Kayıtları Sayısı")]
        public int RegistrationCount { get; set; }
    }
} 