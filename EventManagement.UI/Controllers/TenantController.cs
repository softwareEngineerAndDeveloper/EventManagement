using Microsoft.AspNetCore.Mvc;
using EventManagement.UI.Interfaces;
using EventManagement.UI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;

namespace EventManagement.UI.Controllers
{
    [Authorize]
    public class TenantController : BaseController
    {
        private readonly IApiServiceUI _apiService;

        public TenantController(ILogger<TenantController> logger, IApiServiceUI apiService)
            : base(logger)
        {
            _apiService = apiService;
        }

        // Mevcut tenant bilgilerini göster
        public async Task<IActionResult> Index()
        {
            var tenantResult = await _apiService.GetCurrentTenantAsync();
            
            if (!tenantResult.IsSuccess || tenantResult.Data == null)
            {
                TempData["ErrorMessage"] = tenantResult.Message ?? "Tenant bilgileri alınamadı.";
                return RedirectToAction("Index", "Home");
            }
            
            return View(tenantResult.Data);
        }

        // Tenant değiştirme sayfası
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Switch()
        {
            var tenantResult = await _apiService.GetCurrentTenantAsync();
            
            if (!tenantResult.IsSuccess || tenantResult.Data == null)
            {
                TempData["ErrorMessage"] = tenantResult.Message ?? "Tenant bilgileri alınamadı.";
                return RedirectToAction("Index", "Home");
            }
            
            ViewBag.CurrentTenant = tenantResult.Data;
            
            return View();
        }

        // Tenant değiştirme işlemi
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public IActionResult Switch(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Geçerli bir tenant ID'si belirtilmelidir.";
                return RedirectToAction(nameof(Switch));
            }
            
            // Yeni tenant ID'sini set et
            SetTenantId(tenantId);
            
            TempData["SuccessMessage"] = "Tenant başarıyla değiştirildi.";
            return RedirectToAction("Index", "Home");
        }

        // Test için tenant değiştirme (direkt URL ile)
        [Authorize(Roles = "Administrator")]
        public IActionResult SwitchTo(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Geçerli bir tenant ID'si belirtilmelidir.";
                return RedirectToAction("Index", "Home");
            }
            
            // Yeni tenant ID'sini set et
            SetTenantId(id);
            
            TempData["SuccessMessage"] = $"Tenant ID başarıyla {id} olarak değiştirildi.";
            return RedirectToAction("Index", "Home");
        }
        
        // Test için tenant değiştirme (Subdomain ile)
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> SwitchToSubdomain(string subdomain)
        {
            if (string.IsNullOrEmpty(subdomain))
            {
                TempData["ErrorMessage"] = "Geçerli bir subdomain belirtilmelidir.";
                return RedirectToAction("Index", "Home");
            }
            
            try
            {
                // Tenant resolver service kullanarak subdomain'e karşılık gelen ID'yi bul
                var tenantResolverService = HttpContext.RequestServices.GetRequiredService<ITenantResolverService>();
                var tenantId = await tenantResolverService.GetTenantIdBySubdomainAsync(subdomain);
                
                if (tenantId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = $"'{subdomain}' subdomaini için geçerli bir tenant bulunamadı.";
                    return RedirectToAction("Index", "Home");
                }
                
                // Yeni tenant ID'sini set et
                SetTenantId(tenantId);
                
                TempData["SuccessMessage"] = $"Tenant başarıyla '{subdomain}' olarak değiştirildi. (ID: {tenantId})";
                
                // Subdomain'i URL'de göster
                return Redirect($"/{subdomain}/Home/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant değiştirilirken hata oluştu: {Subdomain}", subdomain);
                TempData["ErrorMessage"] = $"Tenant değiştirilirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
} 