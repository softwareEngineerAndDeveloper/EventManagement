using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }
        
        [MaxLength(200)]
        public required string Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public required string Subdomain { get; set; }
        
        [MaxLength(100)]
        public required string ContactEmail { get; set; }
        
        [MaxLength(20)]
        public required string ContactPhone { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigasyon özellikleri
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
} 