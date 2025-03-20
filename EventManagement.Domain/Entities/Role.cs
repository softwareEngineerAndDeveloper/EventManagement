using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public class Role : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public required string Name { get; set; }
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        // Tenant ilişkisi (Roller tenant'a özel olabilir)
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
        
        // Navigasyon özellikleri
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
} 