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
                
                // Önce basit kullanıcı listesini al
                var usersResult = await _apiService.GetAsync<List<UserListViewModel>>("api/users", token);

                if (!usersResult.Success)
                {
                    TempData["ErrorMessage"] = usersResult.Message ?? "Kullanıcılar getirilirken bir hata oluştu.";
                    return View(new List<UserListViewModel>());
                }

                // Tüm rolleri bir kere al
                var allRolesResult = await _apiService.GetAsync<List<RoleViewModel>>("api/roles", token);
                var allRoles = allRolesResult.Success ? allRolesResult.Data : new List<RoleViewModel>();

                // Rolleri loglayalım
                if (allRoles != null && allRoles.Any())
                {
                    _logger.LogInformation("Sistemde tanımlı roller: {Roles}", string.Join(", ", allRoles.Select(r => $"{r.Name} ({r.Id})")));
                }
                else
                {
                    _logger.LogWarning("Sistemde tanımlı rol bulunamadı!");
                }

                // Her kullanıcı için ayrı ayrı detaylı bilgi al
                var detailedUsers = new List<UserListViewModel>();
                foreach (var basicUser in usersResult.Data)
                {
                    try
                    {
                        // Kullanıcı detayını çek
                        var userDetailResult = await _apiService.GetAsync<UserDetailViewModel>($"api/users/{basicUser.Id}", token);
                        
                        if (userDetailResult.Success && userDetailResult.Data != null)
                        {
                            // UserDetailViewModel'i UserListViewModel'e dönüştür
                            var detailedUser = new UserListViewModel
                            {
                                Id = userDetailResult.Data.Id,
                                FirstName = userDetailResult.Data.FirstName,
                                LastName = userDetailResult.Data.LastName,
                                Email = userDetailResult.Data.Email,
                                PhoneNumber = userDetailResult.Data.PhoneNumber,
                                IsActive = userDetailResult.Data.IsActive,
                                TenantId = userDetailResult.Data.TenantId,
                                TenantName = userDetailResult.Data.TenantName,
                                CreatedDate = userDetailResult.Data.CreatedDate
                            };
                            
                            // Kullanıcı rollerini ayrıca çek
                            var userRolesResult = await _apiService.GetAsync<List<Guid>>($"api/roles/user/{basicUser.Id}", token);
                            if (userRolesResult.Success && userRolesResult.Data != null)
                            {
                                _logger.LogInformation("Kullanıcı {UserId} ({Email}) için dönen rol ID'leri: {RoleIds}", 
                                    basicUser.Id, basicUser.Email, 
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
                                            _logger.LogInformation("Kullanıcı {UserId} için rol eşleşmesi bulundu: {RoleId} => {RoleName}", 
                                                basicUser.Id, roleId, role.Name);
                                        }
                                        else
                                        {
                                            notFoundRoles.Add(roleId);
                                            _logger.LogWarning("Kullanıcı {UserId} için rol ID'si {RoleId} sistemde tanımlı roller arasında bulunamadı!", 
                                                basicUser.Id, roleId);
                                        }
                                    }
                                    
                                    if (notFoundRoles.Any())
                                    {
                                        _logger.LogWarning("Kullanıcı {UserId} için bulunamayan rol ID'leri: {RoleIds}", 
                                            basicUser.Id, string.Join(", ", notFoundRoles));
                                    }
                                    
                                    detailedUser.Roles = roleNames;
                                }
                                else
                                {
                                    detailedUser.Roles = new List<string>();
                                    _logger.LogWarning("Kullanıcı {UserId} için rol bilgisi bulunamadı", basicUser.Id);
                                }
                            }
                            else
                            {
                                detailedUser.Roles = new List<string>();
                                _logger.LogWarning("Kullanıcı {UserId} için rol bilgisi alınamadı: {ErrorMessage}", 
                                    basicUser.Id, userRolesResult.Message ?? "Bilinmeyen hata");
                            }
                            
                            detailedUsers.Add(detailedUser);
                            
                            // Log
                            _logger.LogInformation("Kullanıcı {UserId} detay bilgisi alındı, rolleri: {Roles}", 
                                detailedUser.Id, 
                                detailedUser.Roles.Any() ? string.Join(", ", detailedUser.Roles) : "Rol yok");
                        }
                        else
                        {
                            // Detay bilgisi alınamazsa basit kullanıcı bilgisini kullan
                            detailedUsers.Add(basicUser);
                            _logger.LogWarning("Kullanıcı {UserId} için detay bilgisi alınamadı", basicUser.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kullanıcı {UserId} için detay bilgisi getirilirken hata oluştu", basicUser.Id);
                        detailedUsers.Add(basicUser); // Hata durumunda basit kullanıcı bilgisini ekle
                    }
                }

                return View(detailedUsers);
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
    }
} 