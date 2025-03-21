using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.DTOs
{
    /// <summary>
    /// Rol oluşturma isteği için model
    /// </summary>
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Rol adı gereklidir")]
        [StringLength(50, ErrorMessage = "Rol adı en fazla {1} karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Açıklama en fazla {1} karakter olabilir")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Rol güncelleme isteği için model
    /// </summary>
    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "Rol adı gereklidir")]
        [StringLength(50, ErrorMessage = "Rol adı en fazla {1} karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Açıklama en fazla {1} karakter olabilir")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Rol yanıtı için model
    /// </summary>
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
} 