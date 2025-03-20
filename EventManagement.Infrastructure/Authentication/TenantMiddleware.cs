using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace EventManagement.Infrastructure.Authentication
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        
        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            Tenant? tenant = null;
            var unitOfWork = context.RequestServices.GetRequiredService<IUnitOfWork>();
            
            // X-Tenant-ID header'ını kontrol et
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) && 
                Guid.TryParse(tenantIdHeader.ToString(), out var headerTenantId))
            {
                var tenantsById = await unitOfWork.Tenants.FindAsync(t => t.Id == headerTenantId && t.IsActive);
                tenant = tenantsById?.FirstOrDefault();
            }
            
            // X-Tenant header'ını kontrol et
            if (tenant == null && context.Request.Headers.TryGetValue("X-Tenant", out var tenantSubdomainHeader))
            {
                string tenantSubdomain = tenantSubdomainHeader.ToString();
                var tenantsBySubdomain = await unitOfWork.Tenants.FindAsync(t => t.Subdomain == tenantSubdomain && t.IsActive);
                tenant = tenantsBySubdomain?.FirstOrDefault();
            }
            
            // Subdomain'i kontrol et - localhost için pas geç
            if (tenant == null)
            {
                var host = context.Request.Host.Value;
                
                // localhost ise, varsayılan tenant getirmeye çalış
                if (host.Contains("localhost") || host.Contains("127.0.0.1"))
                {
                    var defaultTenants = await unitOfWork.Tenants.FindAsync(t => t.IsActive);
                    tenant = defaultTenants.FirstOrDefault();
                    
                    // Varsayılan tenant yoksa ama bu bir kimlik doğrulama isteği ise, geçmesine izin ver
                    if (tenant == null && (
                        context.Request.Path.StartsWithSegments("/api/Auth") || 
                        context.Request.Path.StartsWithSegments("/api/Tenant")))
                    {
                        await _next(context);
                        return;
                    }
                }
                else
                {
                    // Normal subdomain kontrolü
                    var subdomain = GetSubdomainFromHost(host);
                    if (!string.IsNullOrEmpty(subdomain))
                    {
                        var tenantsBySubdomain = await unitOfWork.Tenants.FindAsync(t => t.Subdomain == subdomain && t.IsActive);
                        tenant = tenantsBySubdomain.FirstOrDefault();
                    }
                }
            }
            
            // Tenant bulunduysa
            if (tenant != null)
            {
                context.Items["Tenant"] = tenant;
                
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var tenantIdClaim = context.User.FindFirst("tenant_id");
                    if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                    {
                        if (tenantId != tenant.Id)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Tenant mismatch");
                            return;
                        }
                    }
                }
            }
            else if (!context.Request.Path.StartsWithSegments("/api/Auth") && 
                     !context.Request.Path.StartsWithSegments("/api/Tenant"))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Tenant not found");
                return;
            }
            
            await _next(context);
        }
        
        private string? GetSubdomainFromHost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return null;
            
            var parts = host.Split('.');
            
            if (parts.Length < 3)
                return null;
            
            return parts[0];
        }
    }
} 