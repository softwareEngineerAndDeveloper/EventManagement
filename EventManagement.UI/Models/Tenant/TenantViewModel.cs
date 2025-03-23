using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace EventManagement.UI.Models.Tenant
{
    public class TenantViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Tenant adı gereklidir")]
        [StringLength(100, ErrorMessage = "Tenant adı en fazla 100 karakter olabilir")]
        [Display(Name = "İsim")]
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Açıklama gereklidir")]
        [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir")]
        [Display(Name = "Açıklama")]
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Alt alan adı gereklidir")]
        [StringLength(50, ErrorMessage = "Alt alan adı en fazla 50 karakter olabilir")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Alt alan adı sadece harf, rakam ve tire içerebilir")]
        [Display(Name = "Alt Alan Adı")]
        [JsonProperty("subdomain")]
        public string Subdomain { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "İletişim e-postası gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "İletişim E-postası")]
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "İletişim telefonu gereklidir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [Display(Name = "İletişim Telefonu")]
        [JsonProperty("contactPhone")]
        public string ContactPhone { get; set; } = string.Empty;
        
        [Display(Name = "Logo URL")]
        [JsonProperty("logoUrl")]
        public string? LogoUrl { get; set; }
        
        [Display(Name = "Aktif")]
        [JsonProperty("isActive")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Oluşturulma Tarihi")]
        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Düzenlenme Tarihi")]
        [JsonProperty("updatedDate")]
        public DateTime? UpdatedDate { get; set; }
    }
} 