using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.User
{
    public class UserViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Telefon Numarası")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Kullanıcı Adı")]
        public string FullName => $"{FirstName} {LastName}";
        
        public List<string> Roles { get; set; } = new List<string>();
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Son Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }
    }
} 