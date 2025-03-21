using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventManagement.UI.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ILogger<BaseController> _logger;

        public BaseController(ILogger<BaseController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// TenantId değerini Session'dan alır, yoksa Cookie'den almayı dener, 
        /// URL veya subdomain parametresi kontrol eder,
        /// yine bulamazsa varsayılan değeri döndürür.
        /// </summary>
        /// <returns>Tenant ID'si</returns>
        protected Guid GetTenantId()
        {
            // 1. Session'dan TenantId'yi almayı dene
            var tenantIdStr = HttpContext.Session.GetString("TenantId");
            
            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out Guid sessionTenantId))
            {
                _logger.LogDebug("TenantId Session'dan alındı: {TenantId}", sessionTenantId);
                return sessionTenantId;
            }

            // 2. Cookie'den TenantId'yi almayı dene
            if (HttpContext.Request.Cookies.TryGetValue("TenantId", out string cookieTenantIdStr) && 
                !string.IsNullOrEmpty(cookieTenantIdStr) && 
                Guid.TryParse(cookieTenantIdStr, out Guid cookieTenantId))
            {
                _logger.LogDebug("TenantId Cookie'den alındı: {TenantId}", cookieTenantId);
                
                // Session'a kaydedebiliriz gelecekteki istekler için
                HttpContext.Session.SetString("TenantId", cookieTenantIdStr);
                
                return cookieTenantId;
            }

            // 3. URL Routing'den {tenant} parametresini kontrol et
            if (HttpContext.Request.RouteValues.TryGetValue("tenant", out var tenantRouteValue) && 
                tenantRouteValue != null)
            {
                string tenantSubdomain = tenantRouteValue.ToString()!;
                // Burada tenant subdomain'ini ID'ye çevirme işlemi gerekiyor
                // Bu örnek için varsayılan tenant ID döndürüyoruz
                // Gerçek uygulamada TenantResolverService kullanılabilir
                var tenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                
                _logger.LogDebug("TenantId URL parametresinden alındı: {Subdomain} => {TenantId}", tenantSubdomain, tenantId);
                
                // Session'a kaydedebiliriz gelecekteki istekler için
                HttpContext.Session.SetString("TenantId", tenantId.ToString());
                
                return tenantId;
            }
            
            // 4. Host'tan subdomain kontrol et (localhost hariç)
            var host = HttpContext.Request.Host.Value;
            if (!host.Contains("localhost") && !host.Contains("127.0.0.1"))
            {
                var parts = host.Split('.');
                if (parts.Length >= 3) // subdomain.domain.tld
                {
                    string tenantSubdomain = parts[0];
                    if (tenantSubdomain != "www")
                    {
                        // Burada tenant subdomain'ini ID'ye çevirme işlemi gerekiyor
                        // Bu örnek için varsayılan tenant ID döndürüyoruz
                        // Gerçek uygulamada TenantResolverService kullanılabilir
                        var tenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                        
                        _logger.LogDebug("TenantId Subdomain'den alındı: {Subdomain} => {TenantId}", tenantSubdomain, tenantId);
                        
                        // Session'a kaydedebiliriz gelecekteki istekler için
                        HttpContext.Session.SetString("TenantId", tenantId.ToString());
                        
                        return tenantId;
                    }
                }
            }

            // 5. Claims'den TenantId'yi almayı dene (kimlik doğrulama sağlanmışsa)
            if (User.Identity?.IsAuthenticated == true)
            {
                var claimTenantId = User.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (claimTenantId != null && Guid.TryParse(claimTenantId.Value, out Guid userTenantId))
                {
                    _logger.LogDebug("TenantId Claims'den alındı: {TenantId}", userTenantId);
                    
                    // Session'a kaydedebiliriz gelecekteki istekler için
                    HttpContext.Session.SetString("TenantId", claimTenantId.Value);
                    
                    return userTenantId;
                }
            }

            // 6. Hiçbir yerden bulunamadıysa, varsayılan TenantId döndür
            var defaultTenantId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            _logger.LogWarning("TenantId hiçbir yerden bulunamadı. Varsayılan TenantId kullanılıyor: {TenantId}", defaultTenantId);
            
            return defaultTenantId;
        }

        /// <summary>
        /// TenantId'yi Session ve Cookie'ye kaydet
        /// </summary>
        /// <param name="tenantId">Kaydedilecek Tenant ID</param>
        protected void SetTenantId(Guid tenantId)
        {
            var tenantIdStr = tenantId.ToString();
            
            // Session'a kaydet
            HttpContext.Session.SetString("TenantId", tenantIdStr);
            
            // Cookie'ye kaydet (30 gün geçerli)
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax
            };
            
            HttpContext.Response.Cookies.Append("TenantId", tenantIdStr, cookieOptions);
            
            _logger.LogDebug("TenantId Session ve Cookie'ye kaydedildi: {TenantId}", tenantId);
        }

        /// <summary>
        /// Her action çağrılmadan önce çalışır
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            // İstek başlamadan önce loglama yapılabilir
            _logger.LogDebug("Action çağrılıyor: {Controller}/{Action}", 
                context.RouteData.Values["controller"], 
                context.RouteData.Values["action"]);
        }
    }
} 