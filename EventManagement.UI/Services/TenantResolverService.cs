using Microsoft.Extensions.Options;
using EventManagement.UI.Models;
using EventManagement.UI.DTOs;
using EventManagement.UI.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EventManagement.UI.Services
{
    public class TenantResolverService : ITenantResolverService
    {
        private readonly IMemoryCache _cache;
        private readonly IApiServiceUI _apiService;
        private readonly ILogger<TenantResolverService> _logger;

        public TenantResolverService(
            IMemoryCache cache,
            IApiServiceUI apiService,
            ILogger<TenantResolverService> logger)
        {
            _cache = cache;
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Subdomain'e göre TenantId'yi döndürür
        /// </summary>
        public async Task<Guid> GetTenantIdBySubdomainAsync(string subdomain)
        {
            // Cache'den kontrol et
            var cacheKey = $"tenant_{subdomain}";
            if (_cache.TryGetValue<Guid>(cacheKey, out var tenantId) && tenantId != Guid.Empty)
            {
                _logger.LogInformation("Tenant cache'den bulundu: {Subdomain}, TenantId: {TenantId}", subdomain, tenantId);
                return tenantId;
            }

            // API'den tenant bilgisini al
            try
            {
                // TODO: Bu metot henüz API'de tanımlanmamış olabilir, uygulamanıza göre düzenleyin
                var result = await _apiService.GetTenantBySubdomainAsync(subdomain);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // Sonucu cache'e kaydet (1 saat)
                    _cache.Set(cacheKey, result.Data.Id, TimeSpan.FromHours(1));
                    
                    _logger.LogInformation("Tenant API'den bulundu: {Subdomain}, TenantId: {TenantId}", subdomain, result.Data.Id);
                    return result.Data.Id;
                }
                
                _logger.LogWarning("Tenant API'den bulunamadı: {Subdomain}, Hata: {Error}", subdomain, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant sorgulanırken hata oluştu: {Subdomain}", subdomain);
            }
            
            // Varsayılan tenant ID'yi döndür
            return new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        }

        /// <summary>
        /// TenantId'ye göre Tenant bilgilerini döndürür
        /// </summary>
        public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
        {
            // Cache'den kontrol et
            var cacheKey = $"tenant_details_{tenantId}";
            if (_cache.TryGetValue<TenantDto>(cacheKey, out var tenantDto) && tenantDto != null)
            {
                _logger.LogInformation("Tenant detayları cache'den bulundu: {TenantId}", tenantId);
                return tenantDto;
            }

            // API'den tenant bilgisini al
            try
            {
                var result = await _apiService.GetTenantByIdAsync(tenantId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // Sonucu cache'e kaydet (1 saat)
                    _cache.Set(cacheKey, result.Data, TimeSpan.FromHours(1));
                    
                    _logger.LogInformation("Tenant detayları API'den bulundu: {TenantId}", tenantId);
                    return result.Data;
                }
                
                _logger.LogWarning("Tenant detayları API'den bulunamadı: {TenantId}, Hata: {Error}", tenantId, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant detayları sorgulanırken hata oluştu: {TenantId}", tenantId);
            }
            
            return null;
        }
    }
} 