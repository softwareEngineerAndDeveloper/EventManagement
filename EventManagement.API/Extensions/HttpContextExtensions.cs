using EventManagement.Infrastructure.Authentication;
using System.Security.Claims;

namespace EventManagement.API.Extensions
{
    public static class HttpContextExtensions
    {
        public static Guid GetUserId(this HttpContext context)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            
            return Guid.Empty;
        }
        
        public static Guid GetTenantId(this HttpContext context)
        {
            var tenant = context.GetTenant();
            return tenant?.Id ?? Guid.Empty;
        }
    }
} 