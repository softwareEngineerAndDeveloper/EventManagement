using System.ComponentModel.DataAnnotations;

namespace EventManagement.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        
        public required string FirstName { get; set; }
        
        public required string LastName { get; set; }
        
        public required string Email { get; set; }
        
        public required string PhoneNumber { get; set; }
        
        public bool IsActive { get; set; }
        
        public Guid TenantId { get; set; }
        
        public List<RoleDto>? Roles { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    
    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public required string FirstName { get; set; }
        
        [Required]
        [MaxLength(50)]
        public required string LastName { get; set; }
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public required string Email { get; set; }
        
        [MaxLength(20)]
        public required string PhoneNumber { get; set; }
        
        [Required]
        [MinLength(6)]
        public required string Password { get; set; }
        
        [Required]
        [Compare("Password")]
        public required string ConfirmPassword { get; set; }
    }
    
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(50)]
        public required string FirstName { get; set; }
        
        [Required]
        [MaxLength(50)]
        public required string LastName { get; set; }
        
        [MaxLength(20)]
        public required string PhoneNumber { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
        
        [Required]
        public required string Password { get; set; }
        
        public required string Subdomain { get; set; }
    }
    
    public class ChangePasswordDto
    {
        [Required]
        public required string CurrentPassword { get; set; }
        
        [Required]
        [MinLength(6)]
        public required string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword")]
        public required string ConfirmNewPassword { get; set; }
    }
    
    public class AssignRoleDto
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid RoleId { get; set; }
    }
    
    public class RoleDto
    {
        public Guid Id { get; set; }
        
        public required string Name { get; set; }
        
        public string? Description { get; set; }
    }
} 