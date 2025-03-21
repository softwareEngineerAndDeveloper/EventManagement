using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.Models
{
    public class TenantViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Ad")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Subdomain")]
        public string Subdomain { get; set; } = string.Empty;
        
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }
        
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
        
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }
    }
} 