using System.ComponentModel.DataAnnotations;

namespace EventManagement.Application.DTOs
{
    public class CreateTenantDto
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
        [EmailAddress]
        public required string ContactEmail { get; set; }

        [MaxLength(20)]
        public required string ContactPhone { get; set; }
    }

    public class TenantDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Subdomain { get; set; }
        public required string ContactEmail { get; set; }
        public required string ContactPhone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    
    public class UpdateTenantDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }
        
        [MaxLength(200)]
        public required string Description { get; set; }
        
        [MaxLength(100)]
        [EmailAddress]
        public required string ContactEmail { get; set; }
        
        [MaxLength(20)]
        public required string ContactPhone { get; set; }
        
        public bool IsActive { get; set; }
    }
} 