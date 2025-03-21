using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using EventManagement.UI.Models;
using System.Linq;
using EventManagement.UI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventManagement.UI.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;
        private readonly TenantSettings _tenantSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantMiddleware(
            RequestDelegate next,
            ILogger<TenantMiddleware> logger,
            IOptions<TenantSettings> tenantSettings,
            IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _logger = logger;
            _tenantSettings = tenantSettings.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // URL'den tenant bilgisini almayı dene
            var tenantFromPath = GetTenantFromPath(context.Request.Path);
            if (!string.IsNullOrEmpty(tenantFromPath))
            {
                // Tenant bilgisini session ve cookie'ye kaydet
                var tenantId = await GetTenantIdBySubdomain(tenantFromPath);
                if (tenantId != Guid.Empty)
                {
                    _logger.LogInformation("URL'den tenant bulundu: {TenantSubdomain}, TenantId: {TenantId}", tenantFromPath, tenantId);
                    
                    // Session'a kaydet
                    context.Session.SetString("TenantId", tenantId.ToString());
                    
                    // Cookie'ye kaydet (30 gün geçerli)
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    };
                    
                    context.Response.Cookies.Append("TenantId", tenantId.ToString(), cookieOptions);
                    
                    // URL'den tenant parametresini kaldır ve yönlendir
                    var newPath = RemoveTenantFromPath(context.Request.Path, tenantFromPath);
                    if (newPath != context.Request.Path)
                    {
                        context.Response.Redirect(newPath);
                        return;
                    }
                }
            }
            
            // Subdomain'den tenant bilgisini almayı dene
            var tenantFromSubdomain = GetTenantFromSubdomain(context.Request.Host.Value);
            if (!string.IsNullOrEmpty(tenantFromSubdomain) && tenantFromSubdomain != "www")
            {
                // Tenant bilgisini session ve cookie'ye kaydet
                var tenantId = await GetTenantIdBySubdomain(tenantFromSubdomain);
                if (tenantId != Guid.Empty)
                {
                    _logger.LogInformation("Subdomain'den tenant bulundu: {TenantSubdomain}, TenantId: {TenantId}", tenantFromSubdomain, tenantId);
                    
                    // Session'a kaydet
                    context.Session.SetString("TenantId", tenantId.ToString());
                    
                    // Cookie'ye kaydet (30 gün geçerli)
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    };
                    
                    context.Response.Cookies.Append("TenantId", tenantId.ToString(), cookieOptions);
                }
            }
            
            await _next(context);
        }
        
        /// <summary>
        /// URL path'inden tenant bilgisini alır (örn: /tenant1/Home/Index -> tenant1)
        /// </summary>
        private string? GetTenantFromPath(PathString path)
        {
            var pathValue = path.Value;
            if (string.IsNullOrEmpty(pathValue) || pathValue == "/")
                return null;
            
            var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return null;
            
            // İlk segment controller veya area olabilir, kontrol et
            var firstSegment = segments[0].ToLowerInvariant();
            
            // Bilinen controller'lar ve area'lar listesi
            var knownControllersAndAreas = new[] 
            { 
                "home", "event", "admin", "account", "tenant", 
                "user", "role", "auth", "attendee", "error" 
            };
            
            // Eğer ilk segment bilinen bir controller veya area değilse, tenant olabilir
            if (!knownControllersAndAreas.Contains(firstSegment))
            {
                return firstSegment;
            }
            
            return null;
        }
        
        /// <summary>
        /// Host değerinden subdomain'i alır (örn: tenant1.domain.com -> tenant1)
        /// </summary>
        private string? GetTenantFromSubdomain(string host)
        {
            if (string.IsNullOrEmpty(host))
                return null;
            
            // localhost için özel durum, tenant yok
            if (host.Contains("localhost") || host.Contains("127.0.0.1"))
                return null;
            
            var parts = host.Split('.');
            
            if (parts.Length < 3) // Subdomain.domain.tld
                return null;
            
            return parts[0];
        }
        
        /// <summary>
        /// URL'den tenant kısmını kaldırır (örn: /tenant1/Home/Index -> /Home/Index)
        /// </summary>
        private string RemoveTenantFromPath(PathString path, string tenant)
        {
            var pathValue = path.Value;
            if (string.IsNullOrEmpty(pathValue))
                return "/";
            
            var pattern = $"^/{tenant}(/|$)";
            return Regex.Replace(pathValue, pattern, "/", RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Subdomain'e göre tenant ID'sini bulur
        /// Not: Gerçek bir uygulama için, tenant reposity veya cache'den alınmalıdır
        /// </summary>
        private async Task<Guid> GetTenantIdBySubdomain(string subdomain)
        {
            // TenantResolverService üzerinden subdomain'e göre tenant'ı bul
            var tenantResolverService = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ITenantResolverService>();
            return await tenantResolverService.GetTenantIdBySubdomainAsync(subdomain);
        }
    }
    
    // Middleware extension metodları
    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
} 