using EventManagement.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Infrastructure.Authentication
{
    public static class TenantExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TenantMiddleware>();
        }
        
        public static Tenant? GetTenant(this HttpContext context)
        {
            return context.Items["Tenant"] as Tenant;
        }
    }
} 