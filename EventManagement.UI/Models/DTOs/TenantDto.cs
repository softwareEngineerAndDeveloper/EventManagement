using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateTenantDto
    {
        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100, ErrorMessage = "İsim en fazla {3} karakter olabilir")]
        [Display(Name = "Organizasyon Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alt alan adı gereklidir")]
        [RegularExpression(@"^[a-z0-9]+$", ErrorMessage = "Sadece küçük harfler ve rakamlar kullanılabilir")]
        [StringLength(50, ErrorMessage = "Alt alan adı en fazla {1} karakter olabilir")]
        [Display(Name = "Alt Alan Adı")]
        public string Subdomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "İletişim e-postası gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "İletişim E-postası")]
        public string ContactEmail { get; set; } = string.Empty;

        [Display(Name = "İletişim Telefonu")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string ContactPhone { get; set; } = string.Empty;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTenantDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100, ErrorMessage = "İsim en fazla {1} karakter olabilir")]
        [Display(Name = "Organizasyon Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "İletişim e-postası gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "İletişim E-postası")]
        public string ContactEmail { get; set; } = string.Empty;

        [Display(Name = "İletişim Telefonu")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string ContactPhone { get; set; } = string.Empty;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
    }
} 