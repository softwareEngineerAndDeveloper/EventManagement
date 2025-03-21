using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre alanı zorunludur")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        
        public string? ReturnUrl { get; set; }

         // Tenant için subdomain bilgisi
        public string Subdomain { get; set; } = "test";
    }
} 