using EventManagement.UI.Models;
using EventManagement.UI.Models.Role;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Services;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            IUserService userService,
            IAuthService authService,
            ILogger<RoleController> logger)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    _logger.LogError("Geçersiz tenant ID");
                    TempData["Error"] = "Geçersiz tenant bilgisi.";
                    return RedirectToAction("Index", "Dashboard");
                }
                
                var roles = await _roleService.GetAllRolesAsync(token);
                if (!roles.IsSuccess)
                {
                    _logger.LogError("Roller alınırken hata oluştu: {Message}", roles.Message);
                    TempData["Error"] = "Roller alınırken bir hata oluştu: " + roles.Message;
                    return View(new List<RoleViewModel>());
                }

                return View(roles.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roller listelenirken bir hata oluştu");
                TempData["Error"] = "Roller listelenirken bir hata oluştu: " + ex.Message;
                return View(new List<RoleViewModel>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoleViewModel createRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(createRoleDto);
                }

                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // TenantId kontrolü geçici olarak kaldırıldı
                /*
                var tenantId = User.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var parsedTenantId))
                {
                    _logger.LogError("Geçersiz tenant ID");
                    TempData["Error"] = "Geçersiz tenant bilgisi.";
                    return RedirectToAction("Index", "Dashboard");
                }
                */

                // Geçici tenant ID kullanımı
                var parsedTenantId = Guid.NewGuid(); // Geçici olarak rastgele GUID
                createRoleDto.TenantId = parsedTenantId;
                
                _logger.LogInformation("Rol oluşturuluyor: {RoleName}", createRoleDto.Name);
                
                var result = await _roleService.CreateRoleAsync(createRoleDto, token);
                
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Rol başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                
                _logger.LogError("Rol oluşturma başarısız: {Message}", result.Message);
                TempData["Error"] = "Rol oluşturulurken bir hata oluştu: " + result.Message;
                return View(createRoleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol oluşturulurken bir hata oluştu");
                TempData["Error"] = "Rol oluşturulurken bir hata oluştu: " + ex.Message;
                return View(createRoleDto);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                _logger.LogInformation("Rol bilgileri alınıyor. ID: {RoleId}", id);
                
                var role = await _roleService.GetRoleByIdAsync(id, token);
                if (!role.IsSuccess)
                {
                    _logger.LogError("Rol bilgileri alınamadı: {Message}", role.Message);
                    TempData["Error"] = "Rol bilgileri alınırken bir hata oluştu: " + role.Message;
                    return RedirectToAction(nameof(Index));
                }

                var updateRoleDto = new UpdateRoleViewModel
                {
                    Id = role.Data.Id,
                    Name = role.Data.Name,
                    Description = role.Data.Description
                };

                return View(updateRoleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol düzenleme sayfası açılırken bir hata oluştu. Id: {Id}", id);
                TempData["Error"] = "Rol düzenleme sayfası açılırken bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateRoleViewModel updateRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Form doğrulama hataları: {Errors}", string.Join(", ", errors));
                    return View(updateRoleDto);
                }

                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                _logger.LogInformation("Rol güncelleniyor. ID: {RoleId}, Yeni Ad: {RoleName}", updateRoleDto.Id, updateRoleDto.Name);
                
                var result = await _roleService.UpdateRoleAsync(updateRoleDto, token);
                
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Rol başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                
                _logger.LogError("Rol güncelleme başarısız: {Message}", result.Message);
                TempData["Error"] = "Rol güncellenirken bir hata oluştu: " + result.Message;
                return View(updateRoleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncellenirken bir hata oluştu. Id: {Id}", updateRoleDto.Id);
                TempData["Error"] = "Rol güncellenirken bir hata oluştu: " + ex.Message;
                return View(updateRoleDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditViaModal(Guid id, string name, string description)
        {
            _logger.LogInformation("EditViaModal çağrıldı. Rol ID: {RoleId}, Rol Adı: {RoleName}", id, name);

            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Rol adı boş olamaz");
                return Json(new { success = false, message = "Rol adı boş olamaz" });
            }

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token bulunamadı");
                return Json(new { success = false, message = "Oturum zaman aşımına uğradı. Lütfen tekrar giriş yapın." });
            }

            try
            {
                var existingRoleResult = await _roleService.GetRoleByIdAsync(id, token);
                if (existingRoleResult == null || !existingRoleResult.IsSuccess || existingRoleResult.Data == null)
                {
                    _logger.LogWarning("Rol bulunamadı. ID: {RoleId}", id);
                    return Json(new { success = false, message = "Rol bulunamadı" });
                }

                var updateModel = new UpdateRoleViewModel
                {
                    Id = id,
                    Name = name,
                    Description = string.IsNullOrEmpty(description) ? existingRoleResult.Data.Description : description
                };

                _logger.LogInformation("Rol güncelleniyor. ID: {RoleId}, Yeni Ad: {RoleName}", id, name);
                var result = await _roleService.UpdateRoleAsync(updateModel, token);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Rol başarıyla güncellendi. ID: {RoleId}", id);
                    return Json(new { success = true, message = "Rol başarıyla güncellendi" });
                }
                else
                {
                    _logger.LogWarning("Rol güncellenirken hata oluştu. ID: {RoleId}, Hata: {ErrorMessage}", id, result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol güncellenirken beklenmeyen bir hata oluştu. ID: {RoleId}", id);
                return Json(new { success = false, message = "Beklenmeyen bir hata oluştu: " + ex.Message });
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                _logger.LogInformation("Rol silme sayfası açılıyor. ID: {RoleId}", id);
                
                var role = await _roleService.GetRoleByIdAsync(id, token);
                if (!role.IsSuccess)
                {
                    _logger.LogError("Rol bilgileri alınamadı: {Message}", role.Message);
                    TempData["Error"] = "Rol bilgileri alınırken bir hata oluştu: " + role.Message;
                    return RedirectToAction(nameof(Index));
                }

                return View(role.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol silme sayfası açılırken bir hata oluştu. Id: {Id}", id);
                TempData["Error"] = "Rol silme sayfası açılırken bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                // AuthService üzerinden token al
                string token = await _authService.GetTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token alınamadı veya boş. Oturum sonlanmış olabilir.");
                    TempData["Error"] = "Oturumunuz sonlanmış. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                _logger.LogInformation("Rol siliniyor. ID: {RoleId}", id);
                
                var result = await _roleService.DeleteRoleAsync(id, token);
                
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Rol başarıyla silindi.";
                    return RedirectToAction(nameof(Index));
                }
                
                _logger.LogError("Rol silme başarısız: {Message}", result.Message);
                TempData["Error"] = "Rol silinirken bir hata oluştu: " + result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol silinirken bir hata oluştu. Id: {Id}", id);
                TempData["Error"] = "Rol silinirken bir hata oluştu: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 