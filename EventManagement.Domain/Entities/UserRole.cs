using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public class UserRole : BaseEntity
    {
        // Kullanıcı ilişkisi
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        // Rol ilişkisi
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
        
        // Tenant ilişkisi
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
    }
} 