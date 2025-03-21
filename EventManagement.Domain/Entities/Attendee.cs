using System.ComponentModel.DataAnnotations;

namespace EventManagement.Domain.Entities
{
    /// <summary>
    /// Etkinlik katılımcıları için entity sınıfı
    /// </summary>
    public class Attendee : BaseEntity
    {
        [Required]
        public Guid EventId { get; set; }
        public virtual Event Event { get; set; } = null!;

        /// <summary>
        /// Katılımcının adı soyadı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// Katılımcının e-posta adresi
        /// </summary>
        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public required string Email { get; set; }

        /// <summary>
        /// Katılımcının telefon numarası
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        /// <summary>
        /// Katılımcının durumu
        /// 0: Onaylandı
        /// 1: İptal Edildi
        /// 2: Bekleme Listesinde
        /// </summary>
        [Required]
        public int Status { get; set; } = 0;

        /// <summary>
        /// Etkinliğe katıldı mı?
        /// </summary>
        public bool HasAttended { get; set; } = false;

        /// <summary>
        /// Kayıt tarihi
        /// </summary>
        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ek notlar
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Etkinliğe katılımı iptal edildi mi?
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        /// <summary>
        /// Tenant ilişkisi
        /// </summary>
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
    }
} 