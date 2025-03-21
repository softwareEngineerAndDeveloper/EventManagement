using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla {1} karakter olabilir.")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(50, ErrorMessage = "Soyad en fazla {1} karakter olabilir.")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
} 