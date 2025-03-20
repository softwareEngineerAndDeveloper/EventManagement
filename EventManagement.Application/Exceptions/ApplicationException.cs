using System;
using System.Collections.Generic;

namespace EventManagement.Application.Exceptions
{
    public class ApplicationException : Exception
    {
        public ApplicationException(string message) : base(message)
        {
        }
        
        public ApplicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string name, object key) : base($"{name} ({key}) bulunamadı.")
        {
        }
    }
    
    public class ValidationException : ApplicationException
    {
        public ValidationException(string message) : base(message)
        {
            Errors = new List<string>();
        }
        
        public ValidationException(List<string> errors) : base("Bir veya daha fazla doğrulama hatası oluştu.")
        {
            Errors = errors;
        }
        
        public List<string> Errors { get; }
    }
    
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException() : base("Bu işlem için yetkiniz bulunmamaktadır.")
        {
        }
    }
    
    public class TenantMismatchException : ApplicationException
    {
        public TenantMismatchException() : base("Tenant bilgisi eşleşmiyor.")
        {
        }
    }
}

namespace EventManagement.Application.Helpers
{
    public static class PasswordHelper
    {
        // Daha güvenli parola hash'leme için BCrypt kullanımı
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12); // work factor: 12
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
