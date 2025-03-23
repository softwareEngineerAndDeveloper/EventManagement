using EventManagement.UI.Models;
using EventManagement.UI.Models.User;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EventManagement.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;

        public UserController(IApiService apiService, IAuthService authService, ILogger<UserController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = _authService.GetTokenAsync();
                
                // Önce tüm tenant'ları al
                var tenantsResult = await _apiService.GetAsync<List<Models.Tenant.TenantViewModel>>("api/tenants", token);
                
                if (!tenantsResult.Success || tenantsResult.Data == null)
                {
                    _logger.LogWarning("Tenant listesi alınamadı: {0}", tenantsResult.Message ?? "Bilinmeyen hata");
                    TempData["ErrorMessage"] = "Tenant listesi alınamadı. Kullanıcı bilgileri eksik olabilir.";
                }
                
                var tenants = tenantsResult.Success ? tenantsResult.Data : new List<Models.Tenant.TenantViewModel>();
                _logger.LogInformation("Toplam {0} tenant bulundu", tenants.Count);
                
                // Tüm rolleri bir kere al
                var allRolesResult = await _apiService.GetAsync<List<RoleViewModel>>("api/roles", token);
                var allRoles = allRolesResult.Success ? allRolesResult.Data : new List<RoleViewModel>();

                // Rolleri loglayalım
                if (allRoles != null && allRoles.Any())
                {
                    _logger.LogInformation("Sistemde tanımlı roller: {0}", string.Join(", ", allRoles.Select(r => $"{r.Name} ({r.Id})")));
                }
                else
                {
                    _logger.LogWarning("Sistemde tanımlı rol bulunamadı!");
                }
                
                // Tüm kullanıcıları saklamak için liste
                var allUsers = new List<UserListViewModel>();
                
                // Her tenant için kullanıcıları al
                foreach (var tenant in tenants)
                {
                    try
                    {
                        _logger.LogInformation("Tenant {0} ({1}) için kullanıcılar alınıyor...", tenant.Name, tenant.Id);
                        
                        // X-Tenant header ile API çağrısı yapılacak
                        var headers = new Dictionary<string, string>
                        {
                            { "X-Tenant", tenant.Subdomain }
                        };
                        
                        var usersResult = await _apiService.GetAsyncWithHeaders<List<UserListViewModel>>("api/users", token, headers);
                        
                        if (usersResult.Success && usersResult.Data != null)
                        {
                            // Tenant bilgilerini ekle
                            foreach (var user in usersResult.Data)
                            {
                                user.TenantId = tenant.Id;
                                user.TenantName = tenant.Name;
                                
                                // Kullanıcı rollerini ayrıca çek
                                var userRolesResult = await _apiService.GetAsyncWithHeaders<List<Guid>>($"api/roles/user/{user.Id}", token, headers);
                                if (userRolesResult.Success && userRolesResult.Data != null)
                                {
                                    _logger.LogInformation("Kullanıcı {0} ({1}) için dönen rol ID'leri: {2}", 
                                        user.Id, user.Email, 
                                        userRolesResult.Data.Any() ? string.Join(", ", userRolesResult.Data) : "Rol yok");
                                    
                                    if (userRolesResult.Data.Any())
                                    {
                                        var roleNames = new List<string>();
                                        var notFoundRoles = new List<Guid>();
                                        
                                        foreach (var roleId in userRolesResult.Data)
                                        {
                                            var role = allRoles.FirstOrDefault(r => r.Id == roleId);
                                            if (role != null)
                                            {
                                                roleNames.Add(role.Name);
                                                _logger.LogInformation("Kullanıcı {0} için rol eşleşmesi bulundu: {1} => {2}", 
                                                    user.Id, roleId, role.Name);
                                            }
                                            else
                                            {
                                                notFoundRoles.Add(roleId);
                                                _logger.LogWarning("Kullanıcı {0} için rol ID'si {1} sistemde tanımlı roller arasında bulunamadı!", 
                                                    user.Id, roleId);
                                            }
                                        }
                                        
                                        user.Roles = roleNames;
                                    }
                                    else
                                    {
                                        user.Roles = new List<string>();
                                    }
                                }
                                else
                                {
                                    user.Roles = new List<string>();
                                    _logger.LogWarning("Kullanıcı {0} için rol bilgisi alınamadı: {1}", 
                                        user.Id, userRolesResult.Message ?? "Bilinmeyen hata");
                                }
                                
                                // Kullanıcıyı listeye ekle
                                allUsers.Add(user);
                            }
                            
                            _logger.LogInformation("Tenant {0} için {1} kullanıcı alındı", tenant.Name, usersResult.Data.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Tenant {0} için kullanıcı listesi alınamadı: {1}", 
                                tenant.Name, usersResult.Message ?? "Bilinmeyen hata");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tenant {0} için kullanıcı listesi alınırken hata oluştu", tenant.Name);
                    }
                }
                
                _logger.LogInformation("Toplam {0} kullanıcı bulundu", allUsers.Count);
                return View(allUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi getirilirken hata oluştu");
                TempData["ErrorMessage"] = "Kullanıcı listesi getirilirken bir hata oluştu.";
                return View(new List<UserListViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<List<UserListViewModel>>("api/users", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar getirilirken hata oluştu");
                return Json(new { success = false, message = "Kullanıcılar alınırken bir hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<UserDetailViewModel>($"api/users/{id}", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, data = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı detayı getirilirken hata oluştu. UserId: {UserId}", id);
                return Json(new { success = false, message = "Kullanıcı detayı alınırken bir hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<List<RoleViewModel>>("api/roles", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, roles = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roller getirilirken hata oluştu");
                return Json(new { success = false, message = "Roller alınırken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { 
                        success = false, 
                        message = "Lütfen tüm gerekli alanları doldurun.", 
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var token = _authService.GetTokenAsync();
                var result = await _apiService.PostAsync<UserDetailViewModel>("api/users", model, token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                _logger.LogInformation("Yeni kullanıcı oluşturuldu. UserId: {UserId}", result.Data?.Id);
                return Json(new { success = true, message = "Kullanıcı başarıyla oluşturuldu.", userId = result.Data?.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu");
                return Json(new { success = false, message = "Kullanıcı oluşturulurken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { 
                        success = false, 
                        message = "Lütfen tüm gerekli alanları doldurun.", 
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var token = _authService.GetTokenAsync();
                var result = await _apiService.PutAsync<UserDetailViewModel>($"api/users/{model.Id}", model, token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                _logger.LogInformation("Kullanıcı güncellendi. UserId: {UserId}", model.Id);
                return Json(new { success = true, message = "Kullanıcı başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu. UserId: {UserId}", model.Id);
                return Json(new { success = false, message = "Kullanıcı güncellenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(Guid userId, List<Guid> roleIds)
        {
            try
            {
                _logger.LogInformation("Kullanıcı rolleri güncelleme isteği alındı. UserId: {UserId}", userId);
                
                var model = new UserRolesUpdateViewModel
                {
                    UserId = userId,
                    RoleIds = roleIds
                };

                var token = _authService.GetTokenAsync();
                var result = await _apiService.PostAsync<bool>($"api/users/{userId}/roles", model, token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, message = "Kullanıcı rolleri başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı rolleri güncellenirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "Kullanıcı rolleri güncellenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.DeleteAsync<bool>($"api/users/{id}", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                _logger.LogInformation("Kullanıcı silindi. UserId: {UserId}", id);
                return Json(new { success = true, message = "Kullanıcı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu. UserId: {UserId}", id);
                return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRoles(Guid id)
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<List<Guid>>($"api/roles/user/{id}", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, roles = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı rolleri getirilirken hata oluştu. UserId: {UserId}", id);
                return Json(new { success = false, message = "Kullanıcı rolleri alınırken bir hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants()
        {
            try
            {
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<List<Models.Tenant.TenantViewModel>>("api/tenants", token);

                if (!result.Success)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new { success = true, tenants = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenantlar getirilirken hata oluştu");
                return Json(new { success = false, message = "Tenantlar alınırken bir hata oluştu." });
            }
        }
    }
} 