using EventManagement.Application.DTOs;

namespace EventManagement.Application.Interfaces
{
    public interface ITenantService
    {
        Task<ResponseDto<List<TenantDto>>> GetAllTenantsAsync();
        Task<ResponseDto<TenantDto>> GetTenantByIdAsync(Guid id);
        Task<ResponseDto<TenantDto>> GetTenantBySubdomainAsync(string subdomain);
        Task<ResponseDto<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto);
        Task<ResponseDto<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantDto updateTenantDto);
        Task<ResponseDto<bool>> DeleteTenantAsync(Guid id);
    }
} 