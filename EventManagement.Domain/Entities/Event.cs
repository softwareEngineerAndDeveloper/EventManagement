using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    public enum EventStatus
    {
        Pending,    // Onay bekliyor
        Approved,   // Onaylandı
        Rejected,   // Reddedildi
        Cancelled   // İptal edildi
    }

    public class Event : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }
        
        [MaxLength(500)]
        public required string Description { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(200)]
        public required string Location { get; set; }
        
        public int? MaxAttendees { get; set; }
        
        public int? Capacity { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        public bool IsCancelled { get; set; } = false;
        
        [Required]
        public EventStatus Status { get; set; } = EventStatus.Pending;
        
        [Required]
        public Guid CreatorId { get; set; }  // Etkinliği oluşturan kullanıcı
        public virtual User Creator { get; set; } = null!;
        
        // Tenant ilişkisi
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
        
        // Navigasyon özellikleri
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        
        // Katılımcılar - Attendees tablosu ile ilişki
        public virtual ICollection<Attendee> Attendees { get; set; } = new List<Attendee>();
    }
} 