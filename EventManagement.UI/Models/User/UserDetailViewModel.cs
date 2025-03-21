using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.User
{
    public class UserDetailViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Tenant ID")]
        public Guid TenantId { get; set; }
        
        [Display(Name = "Tenant")]
        public string TenantName { get; set; } = string.Empty;
        
        [Display(Name = "Roller")]
        public List<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>();
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }
    }

    public class UserUpdateViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Ad alanı zorunludur")]
        [Display(Name = "Ad")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Soyad alanı zorunludur")]
        [Display(Name = "Soyad")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "E-posta alanı zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Telefon")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class UserRolesUpdateViewModel
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }

    public class RoleViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Rol Adı")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
    }
} 