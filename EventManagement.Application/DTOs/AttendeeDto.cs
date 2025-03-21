using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Application.DTOs
{
    /// <summary>
    /// Etkinlik katılımcı DTO
    /// </summary>
    public class AttendeeDto
    {
        /// <summary>
        /// Katılımcı ID
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Etkinlik ID
        /// </summary>
        public Guid EventId { get; set; }
        
        /// <summary>
        /// Katılımcı adı
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// E-posta adresi
        /// </summary>
        public required string Email { get; set; }
        
        /// <summary>
        /// Telefon numarası (opsiyonel)
        /// </summary>
        public required string Phone { get; set; }
        
        /// <summary>
        /// Katılımcı durumu (0: Beklemede, 1: Onaylandı, 2: İptal edildi)
        /// </summary>
        public required int Status { get; set; }
        
        /// <summary>
        /// Etkinliğe katılıp katılmadığı   
        /// </summary>
        public bool HasAttended { get; set; }
        
        /// <summary>
        /// Kayıt tarihi
        /// </summary>
        public DateTime RegistrationDate { get; set; }
        
        /// <summary>
        /// Katılımcı hakkında notlar
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Kayıt iptal edildi mi
        /// </summary>
        public bool IsCancelled { get; set; }
    }

    /// <summary>
    /// Yeni katılımcı oluşturmak için kullanılan DTO sınıfı
    /// </summary>
    public class CreateAttendeeDto
    {
        /// <summary>
        /// Etkinlik ID'si
        /// </summary>
        [Required(ErrorMessage = "Etkinlik ID'si gereklidir")]
        public Guid EventId { get; set; }

        /// <summary>
        /// Katılımcı adı soyadı
        /// </summary>
        [Required(ErrorMessage = "Ad soyad gereklidir")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir")]
        public required string Name { get; set; }

        /// <summary>
        /// Katılımcı e-posta adresi
        /// </summary>
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
        public required string Email { get; set; }

        /// <summary>
        /// Katılımcı telefon numarası
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        public required string Phone { get; set; }

        /// <summary>
        /// Ek notlar
        /// </summary>
        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Katılımcı bilgilerini güncellemek için kullanılan DTO sınıfı
    /// </summary>
    public class UpdateAttendeeDto
    {
        /// <summary>
        /// Katılımcı adı soyadı
        /// </summary>
        [Required(ErrorMessage = "Ad soyad gereklidir")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir")]
        public required string Name { get; set; }

        /// <summary>
        /// Katılımcı e-posta adresi
        /// </summary>
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir")]
        public required string Email { get; set; }

        /// <summary>
        /// Katılımcı telefon numarası
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        public required string Phone { get; set; }

        /// <summary>
        /// Katılımcının durumu (0: Onaylandı, 1: İptal Edildi, 2: Bekleme Listesinde)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Katılımcı etkinliğe katıldı mı
        /// </summary>
        public bool HasAttended { get; set; }

        /// <summary>
        /// Ek notlar
        /// </summary>
        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Katılımcı arama kriterleri için kullanılan DTO sınıfı
    /// </summary>
    public class SearchAttendeeDto
    {
        /// <summary>
        /// Etkinlik ID'si
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Katılımcı adı soyadı
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Katılımcı e-posta adresi
        /// </summary>
        public required string Email { get; set; }
        }
} 