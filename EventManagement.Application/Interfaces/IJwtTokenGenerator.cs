using EventManagement.Domain.Entities;

namespace EventManagement.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        Task<string> GenerateTokenAsync(User user);
    }
} 