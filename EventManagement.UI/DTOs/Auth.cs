using System.ComponentModel.DataAnnotations;

namespace EventManagement.UI.DTOs
{
    /// <summary>
    /// Giriş yapma isteği için model
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subdomain bilgisi gereklidir")]
        public string Subdomain { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Kayıt olma isteği için model
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100, ErrorMessage = "İsim en fazla {1} karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim gereklidir")]
        [StringLength(100, ErrorMessage = "Soyisim en fazla {1} karakter olabilir")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla {1} karakter olabilir")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla {1} karakter olabilir")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az {2} karakter olmalıdır")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tenant ID gereklidir")]
        public Guid TenantId { get; set; }
    }

    /// <summary>
    /// Şifre değiştirme isteği için model
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre gereklidir")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifre en az {2} karakter olmalıdır")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifreler eşleşmiyor")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Profil güncelleme isteği için model
    /// </summary>
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100, ErrorMessage = "İsim en fazla {1} karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim gereklidir")]
        [StringLength(100, ErrorMessage = "Soyisim en fazla {1} karakter olabilir")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla {1} karakter olabilir")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla {1} karakter olabilir")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Telefon numarası en fazla {1} karakter olabilir")]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Kullanıcı güncelleme isteği için model
    /// </summary>
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "İsim gereklidir")]
        [StringLength(100, ErrorMessage = "İsim en fazla {1} karakter olabilir")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim gereklidir")]
        [StringLength(100, ErrorMessage = "Soyisim en fazla {1} karakter olabilir")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla {1} karakter olabilir")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta en fazla {1} karakter olabilir")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Telefon numarası en fazla {1} karakter olabilir")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public List<string> RoleNames { get; set; } = new();
    }

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

    /// <summary>
    /// Kimlik doğrulama yanıtı için model
    /// </summary>
    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime? TokenExpiration { get; set; }
    }
} 