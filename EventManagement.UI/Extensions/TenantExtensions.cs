using Microsoft.AspNetCore.Http;
using System;

namespace EventManagement.UI.Extensions
{
    /// <summary>
    /// Tenant ile ilgili genişletme metotları
    /// </summary>
    public static class TenantExtensions
    {
        /// <summary>
        /// HttpContext'ten TenantId değerini okur. Session > Cookie > Claims > Default sıralamasını takip eder.
        /// </summary>
        /// <param name="context">HTTP bağlamı</param>
        /// <param name="logger">İsteğe bağlı logger</param>
        /// <returns>Tenant ID</returns>
        public static Guid GetTenantId(this HttpContext context, ILogger logger = null)
        {
            // 1. Session'dan TenantId'yi almayı dene
            var tenantIdStr = context.Session.GetString("TenantId");
            
            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out Guid sessionTenantId))
            {
                logger?.LogDebug("TenantId Session'dan alındı: {TenantId}", sessionTenantId);
                return sessionTenantId;
            }

            // 2. Cookie'den TenantId'yi almayı dene
            if (context.Request.Cookies.TryGetValue("TenantId", out string cookieTenantIdStr) && 
                !string.IsNullOrEmpty(cookieTenantIdStr) && 
                Guid.TryParse(cookieTenantIdStr, out Guid cookieTenantId))
            {
                logger?.LogDebug("TenantId Cookie'den alındı: {TenantId}", cookieTenantId);
                
                // Session'a kaydedebiliriz gelecekteki istekler için
                context.Session.SetString("TenantId", cookieTenantIdStr);
                
                return cookieTenantId;
            }

            // 3. Claims'den TenantId'yi almayı dene (kimlik doğrulama sağlanmışsa)
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claimTenantId = context.User.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (claimTenantId != null && Guid.TryParse(claimTenantId.Value, out Guid userTenantId))
                {
                    logger?.LogDebug("TenantId Claims'den alındı: {TenantId}", userTenantId);
                    
                    // Session'a kaydedebiliriz gelecekteki istekler için
                    context.Session.SetString("TenantId", claimTenantId.Value);
                    
                    return userTenantId;
                }
            }

            // 4. Hiçbir yerden bulunamadıysa, varsayılan TenantId döndür
            var defaultTenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            logger?.LogWarning("TenantId hiçbir yerden bulunamadı. Varsayılan TenantId kullanılıyor: {TenantId}", defaultTenantId);
            
            return defaultTenantId;
        }

        /// <summary>
        /// HttpContext için TenantId değerini ayarlar, hem Session hem Cookie'de
        /// </summary>
        /// <param name="context">HTTP bağlamı</param>
        /// <param name="tenantId">Ayarlanacak TenantId</param>
        /// <param name="logger">İsteğe bağlı logger</param>
        public static void SetTenantId(this HttpContext context, Guid tenantId, ILogger logger = null)
        {
            var tenantIdStr = tenantId.ToString();
            
            // Session'a kaydet
            context.Session.SetString("TenantId", tenantIdStr);
            
            // Cookie'ye kaydet (30 gün geçerli)
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax
            };
            
            context.Response.Cookies.Append("TenantId", tenantIdStr, cookieOptions);
            
            logger?.LogDebug("TenantId Session ve Cookie'ye kaydedildi: {TenantId}", tenantId);
        }
    }
} 