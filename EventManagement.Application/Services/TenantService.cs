using EventManagement.Application.DTOs;
using EventManagement.Application.Exceptions;
using EventManagement.Application.Interfaces;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;

namespace EventManagement.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TenantService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<List<TenantDto>>> GetAllTenantsAsync()
        {
            var tenants = await _unitOfWork.Tenants.GetAllAsync();
            var tenantDtos = tenants.Select(t => MapToDto(t)).ToList();
            
            return ResponseDto<List<TenantDto>>.Success(tenantDtos);
        }

        public async Task<ResponseDto<TenantDto>> GetTenantByIdAsync(Guid id)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(id);
            
            if (tenant == null)
                throw new NotFoundException(nameof(Tenant), id);
                
            return ResponseDto<TenantDto>.Success(MapToDto(tenant));
        }

        public async Task<ResponseDto<TenantDto>> GetTenantBySubdomainAsync(string subdomain)
        {
            var tenants = await _unitOfWork.Tenants.FindAsync(t => t.Subdomain == subdomain && t.IsActive);
            var tenant = tenants.FirstOrDefault();
            
            if (tenant == null)
                throw new NotFoundException(nameof(Tenant), subdomain);
                
            return ResponseDto<TenantDto>.Success(MapToDto(tenant));
        }

        public async Task<ResponseDto<TenantDto>> CreateTenantAsync(CreateTenantDto createTenantDto)
        {
            var existingTenants = await _unitOfWork.Tenants.FindAsync(t => t.Subdomain == createTenantDto.Subdomain);
            
            if (existingTenants.Any())
                throw new ValidationException($"Subdomain '{createTenantDto.Subdomain}' zaten kullanımda.");
                
            var tenant = new Tenant
            {
                Name = createTenantDto.Name,
                Description = createTenantDto.Description,
                Subdomain = createTenantDto.Subdomain,
                ContactEmail = createTenantDto.ContactEmail,
                ContactPhone = createTenantDto.ContactPhone,
                IsActive = true
            };
            
            await _unitOfWork.Tenants.AddAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<TenantDto>.Success(MapToDto(tenant));
        }

        public async Task<ResponseDto<TenantDto>> UpdateTenantAsync(Guid id, UpdateTenantDto updateTenantDto)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(id);
            
            if (tenant == null)
                throw new NotFoundException(nameof(Tenant), id);
                
            tenant.Name = updateTenantDto.Name;
            tenant.Description = updateTenantDto.Description;
            tenant.ContactEmail = updateTenantDto.ContactEmail;
            tenant.ContactPhone = updateTenantDto.ContactPhone;
            tenant.IsActive = updateTenantDto.IsActive;
            
            await _unitOfWork.Tenants.UpdateAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<TenantDto>.Success(MapToDto(tenant));
        }

        public async Task<ResponseDto<bool>> DeleteTenantAsync(Guid id)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(id);
            
            if (tenant == null)
                throw new NotFoundException(nameof(Tenant), id);
                
            await _unitOfWork.Tenants.DeleteAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            
            return ResponseDto<bool>.Success(true);
        }

        private TenantDto MapToDto(Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Description = tenant.Description,
                Subdomain = tenant.Subdomain,
                ContactEmail = tenant.ContactEmail,
                ContactPhone = tenant.ContactPhone,
                IsActive = tenant.IsActive,
                CreatedDate = tenant.CreatedDate,
                UpdatedDate = tenant.UpdatedDate
            };
        }
    }
} 