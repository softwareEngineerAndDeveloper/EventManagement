using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.User
{
    public class UserListViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
        
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Roller")]
        public List<string> Roles { get; set; } = new List<string>();
        
        [Display(Name = "Tenant ID")]
        public Guid TenantId { get; set; }
        
        [Display(Name = "Tenant")]
        public string TenantName { get; set; } = string.Empty;
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
    }
} 