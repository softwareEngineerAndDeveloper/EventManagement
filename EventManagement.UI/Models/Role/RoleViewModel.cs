using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models.Role
{
    public class RoleViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Rol adı gereklidir")]
        [Display(Name = "Rol Adı")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public Guid TenantId { get; set; }
    }
    
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Rol adı gereklidir")]
        [Display(Name = "Rol Adı")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
        
        public Guid TenantId { get; set; }
    }
    
    public class UpdateRoleViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Rol adı gereklidir")]
        [Display(Name = "Rol Adı")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;
    }
} 