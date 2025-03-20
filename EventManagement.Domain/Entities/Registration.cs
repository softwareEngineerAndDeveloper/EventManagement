using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public enum RegistrationStatus
    {
        Waiting,
        Confirmed,
        Cancelled
    }

    public class Registration : BaseEntity
    {
        [Required]
        public Guid EventId { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        [Required]
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }
        
        public bool HasAttended { get; set; } = false;
        
        public bool IsCancelled { get; set; } = false;

        [Required]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Waiting;
    }
} 