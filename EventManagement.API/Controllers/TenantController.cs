using EventManagement.Application.DTOs;
using EventManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using EventManagement.API.Extensions;

namespace EventManagement.API.Controllers
{
    [Route("api/tenants")]
    public class TenantController : BaseApiController
    {
        private readonly ITenantService _tenantService;
        
        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseDto<List<TenantDto>>>> GetAllTenants()
        {
            var result = await _tenantService.GetAllTenantsAsync();
            return Ok(result);
        }
        
        [HttpGet("current")]
        public async Task<ActionResult<ResponseDto<TenantDto>>> GetCurrentTenant()
        {
            var tenantId = GetTenantId();
            var result = await _tenantService.GetTenantByIdAsync(tenantId);
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseDto<TenantDto>>> GetTenantById(Guid id)
        {
            var result = await _tenantService.GetTenantByIdAsync(id);
            return Ok(result);
        }
        
        [HttpGet("subdomain/{subdomain}")]
        public async Task<ActionResult<ResponseDto<TenantDto>>> GetTenantBySubdomain(string subdomain)
        {
            var result = await _tenantService.GetTenantBySubdomainAsync(subdomain);
            return Ok(result);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseDto<TenantDto>>> CreateTenant(CreateTenantDto createTenantDto)
        {
            var result = await _tenantService.CreateTenantAsync(createTenantDto);
            if (result.Data == null)
            {
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetTenantById), new { id = result.Data.Id }, result);
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseDto<TenantDto>>> UpdateTenant(Guid id, UpdateTenantDto updateTenantDto)
        {
            var result = await _tenantService.UpdateTenantAsync(id, updateTenantDto);
            return Ok(result);
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseDto<bool>>> DeleteTenant(Guid id)
        {
            var result = await _tenantService.DeleteTenantAsync(id);
            return Ok(result);
        }
    }
} 