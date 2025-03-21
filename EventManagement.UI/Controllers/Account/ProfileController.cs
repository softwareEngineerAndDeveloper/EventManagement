using EventManagement.UI.Models.Auth;
using EventManagement.UI.Models.Shared;
using EventManagement.UI.Models.User;
using EventManagement.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace EventManagement.UI.Controllers.Account
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IAuthService _authService;
        private readonly IApiService _apiService;

        public ProfileController(ILogger<ProfileController> logger, IAuthService authService, IApiService apiService)
        {
            _logger = logger;
            _authService = authService;
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Kullanıcı bilgilerini API'den al
                var token = _authService.GetTokenAsync();
                var result = await _apiService.GetAsync<UserDto>("api/users/me", token);

                if (!result.Success)
                {
                    _logger.LogWarning("Profil bilgileri alınamadı: {Message}", result.Message);
                    TempData["ErrorMessage"] = result.Message;
                    return View(new ProfileViewModel());
                }

                // Kullanıcı bilgilerini ViewModel'e dönüştür
                var profileModel = new ProfileViewModel
                {
                    Id = result.Data.Id,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    Email = result.Data.Email,
                    PhoneNumber = result.Data.PhoneNumber,
                    CreatedDate = result.Data.CreatedDate,
                    UpdatedDate = result.Data.UpdatedDate,
                    IsActive = result.Data.IsActive
                };

                // Kullanıcı rol bilgisini session'dan al
                profileModel.Roles = new List<string>();
                if (User.IsInRole("Admin"))
                {
                    profileModel.Roles.Add("Admin");
                }
                else if (User.IsInRole("Manager"))
                {
                    profileModel.Roles.Add("Manager");
                }
                else if (User.IsInRole("User"))
                {
                    profileModel.Roles.Add("User");
                }

                return View(profileModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil sayfası yüklenirken hata oluştu");
                TempData["ErrorMessage"] = "Profil bilgileri yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.";
                return View(new ProfileViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // Kullanıcı bilgilerini güncelle
                var updateModel = new UpdateUserDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = model.IsActive
                };

                var token = _authService.GetTokenAsync();
                var result = await _apiService.PutAsync<UserDto>("api/users/me", updateModel, token);

                if (!result.Success)
                {
                    _logger.LogWarning("Profil güncellenemedi: {Message}", result.Message);
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View("Index", model);
                }

                _logger.LogInformation("Kullanıcı profili güncellendi: {Email}", model.Email);
                TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";

                // Güncellenen bilgileri göstermek için Index sayfasına yönlendir
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil güncellenirken hata oluştu");
                ModelState.AddModelError(string.Empty, "Profil güncellenirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                return View("Index", model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Şifre değişikliği için API'yi çağır
                var changePasswordDto = new ChangePasswordDto
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmNewPassword = model.ConfirmNewPassword
                };

                var token = _authService.GetTokenAsync();
                var result = await _apiService.PostAsync<bool>("api/auth/change-password", changePasswordDto, token);

                if (!result.Success)
                {
                    _logger.LogWarning("Şifre değiştirilemedi: {Message}", result.Message);
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                _logger.LogInformation("Kullanıcı şifresi değiştirildi");
                TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirilirken hata oluştu");
                ModelState.AddModelError(string.Empty, "Şifre değiştirilirken bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                return View(model);
            }
        }
    }

    // API modellerini burada tanımlıyoruz (Gerçek uygulamada ayrı dosyalara taşıyabilirsiniz)
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid TenantId { get; set; }
        public List<RoleDto>? Roles { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
} 