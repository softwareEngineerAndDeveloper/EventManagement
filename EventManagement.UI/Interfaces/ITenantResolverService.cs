using EventManagement.UI.DTOs;

namespace EventManagement.UI.Interfaces
{
    public interface ITenantResolverService
    {
        /// <summary>
        /// Subdomain'e göre TenantId'yi döndürür
        /// </summary>
        Task<Guid> GetTenantIdBySubdomainAsync(string subdomain);
        
        /// <summary>
        /// TenantId'ye göre Tenant bilgilerini döndürür
        /// </summary>
        Task<TenantDto?> GetTenantByIdAsync(Guid tenantId);
    }
} 