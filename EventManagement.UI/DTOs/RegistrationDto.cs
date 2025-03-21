using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.DTOs
{
    public class RegistrationDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string ParticipantPhone { get; set; } = string.Empty;
        public int Status { get; set; } // 0: Onaylandı, 1: İptal Edildi, 2: Bekleme Listesinde
        public string EventTitle { get; set; } = string.Empty;
    }

    public class CreateRegistrationDto
    {
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "Katılımcı adı gereklidir")]
        [Display(Name = "Adınız Soyadınız")]
        public string ParticipantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string ParticipantEmail { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string ParticipantPhone { get; set; } = string.Empty;
    }

    public class UpdateRegistrationDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Katılımcı adı gereklidir")]
        [Display(Name = "Adınız Soyadınız")]
        public string ParticipantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string ParticipantEmail { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string ParticipantPhone { get; set; } = string.Empty;

        [Display(Name = "Durum")]
        public int Status { get; set; }
    }
}