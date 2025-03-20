using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public required string FirstName { get; set; }
        
        [Required]
        [MaxLength(50)]
        public required string LastName { get; set; }
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public required string Email { get; set; }
        
        [MaxLength(20)]
        public required string PhoneNumber { get; set; }
        
        [Required]
        public required string PasswordHash { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Tenant ilişkisi
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
        
        // Navigasyon özellikleri
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        
        // Rol ilişkisi
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
} 