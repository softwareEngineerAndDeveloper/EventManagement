using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.User
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Ad alanı zorunludur")]
        [Display(Name = "Ad")]
        [MaxLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Soyad alanı zorunludur")]
        [Display(Name = "Soyad")]
        [MaxLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "E-posta")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [Display(Name = "Telefon Numarası")]
        [MaxLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        [RegularExpression(@"^[0-9+\s\-()]+$", ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Kullanıcı Adı")]
        public string FullName => $"{FirstName} {LastName}";
        
        public List<string> Roles { get; set; } = new List<string>();
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Son Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur")]
        [Display(Name = "Mevcut Şifre")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Yeni şifre zorunludur")]
        [Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
        [Display(Name = "Yeni Şifre Tekrar")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
} 