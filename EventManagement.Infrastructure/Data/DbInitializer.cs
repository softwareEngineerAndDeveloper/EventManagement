using EventManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;
using Microsoft.Data.SqlClient;

namespace EventManagement.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, ILogger<ApplicationDbContext> logger)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Veritabanını oluştur veya migration'ları uygula
                await dbContext.Database.MigrateAsync();
                
                // Tenant kontrolü ve gerekiyorsa oluşturma
                var defaultTenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Subdomain == "test");
                
                if (defaultTenant == null)
                {
                    logger.LogInformation("'test' tenant'ı oluşturuluyor...");
                    
                    defaultTenant = new Tenant
                    {
                        Name = "Varsayılan Organizasyon",
                        Description = "Sistem varsayılan organizasyonu",
                        Subdomain = "test",
                        ContactEmail = "admin@eventmanagement.com",
                        ContactPhone = "1234567890",
                        IsActive = true
                    };
                    
                    dbContext.Tenants.Add(defaultTenant);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation($"Varsayılan tenant oluşturuldu. ID: {defaultTenant.Id}");
                    
                    // Varsayılan rolleri oluştur
                    logger.LogInformation("Varsayılan roller oluşturuluyor...");
                    
                    await CreateDefaultRolesAsync(dbContext, defaultTenant, logger);
                    
                    // Test kullanıcıları oluştur
                    await CreateTestUsersAsync(dbContext, defaultTenant, logger);
                }
                else
                {
                    logger.LogInformation($"Var olan tenant: {defaultTenant.Name}, ID: {defaultTenant.Id}");
                    
                    // Rolleri kontrol et ve gerekirse oluştur
                    await CreateDefaultRolesAsync(dbContext, defaultTenant, logger);
                    
                    // Test kullanıcıları oluştur (eğer yoksa)
                    await CreateTestUsersAsync(dbContext, defaultTenant, logger);
                }
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    // Benzersizlik kısıtlaması hatası (duplicate key)
                    logger.LogWarning("Veritabanı başlatılırken benzersizlik kısıtlaması hatası oluştu. Devam edilebilir: {Message}", sqlEx.Message);
                    // Uygulama başlatılabilir, bu öldürücü bir hata değil
                }
                else
                {
                    logger.LogError(ex, "Veritabanı başlatılırken güncelleme hatası oluştu");
                    throw; // Ciddi veritabanı hatası, uygulamayı başlatmama
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Veritabanı başlatılırken bir hata oluştu");
                throw; // Genel hata, uygulamayı başlatmama
            }
        }
        
        private static async Task ResetUsersAndCreateAdminAsync(ApplicationDbContext dbContext, Tenant tenant, ILogger logger)
        {
            logger.LogInformation("Tüm kullanıcılar siliniyor ve admin kullanıcısı oluşturuluyor...");
            
            // UserRoles tablosunu temizle
            var userRoles = await dbContext.UserRoles.ToListAsync();
            if (userRoles.Any())
            {
                dbContext.UserRoles.RemoveRange(userRoles);
                await dbContext.SaveChangesAsync();
                logger.LogInformation($"{userRoles.Count} kullanıcı rolü silindi");
            }
            
            // Kullanıcıları sil (soft delete)
            var users = await dbContext.Users.ToListAsync();
            foreach (var user in users)
            {
                if (user.Email == "admin@etkinlikyonetimi.com" && user.TenantId == tenant.Id)
                {
                    dbContext.Users.Remove(user);
                    logger.LogInformation($"Admin kullanıcısı tamamen silindi: {user.Email}");
                }
                else
                {
                    user.IsDeleted = true;
                    dbContext.Users.Update(user);
                }
            }
            await dbContext.SaveChangesAsync();
            logger.LogInformation($"{users.Count} kullanıcı soft delete işlemi ile silindi");
            
            // Rolleri kontrol et ve gerekirse oluştur
            await CreateDefaultRolesAsync(dbContext, tenant, logger);
            
            // Test kullanıcıları oluştur (eğer yoksa)
            await CreateTestUsersAsync(dbContext, tenant, logger);
        }
        
        private static async Task CreateTestUsersAsync(ApplicationDbContext dbContext, Tenant tenant, ILogger logger)
        {
            logger.LogInformation("Test kullanıcıları kontrol ediliyor...");
            
            try
            {
                // Admin rolünü bul
                var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.TenantId == tenant.Id);
                if (adminRole == null)
                {
                    logger.LogWarning("Admin rolü bulunamadı!");
                    return;
                }
                
                // Test admin kullanıcısı - fikri@etkinlikyonetimi.com
                bool adminUserExists = await dbContext.Users.AnyAsync(u => u.Email == "admin@etkinlikyonetimi.com" && u.TenantId == tenant.Id);
                
                if (!adminUserExists)
                {
                    logger.LogInformation("Admin test kullanıcısı oluşturuluyor: admin@etkinlikyonetimi.com");
                    
                    var adminUser = new User
                    {
                        FirstName = "Fikri",
                        LastName = "Yönetici",
                        Email = "admin@etkinlikyonetimi.com",
                        PasswordHash = BC.HashPassword("123456"),
                        PhoneNumber = "5551234567",
                        TenantId = tenant.Id,
                        IsActive = true
                    };
                    
                    dbContext.Users.Add(adminUser);
                    await dbContext.SaveChangesAsync();
                    
                    // Admin rolünü ata
                    var userRole = new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id,
                        TenantId = tenant.Id
                    };
                    
                    dbContext.UserRoles.Add(userRole);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation($"Test admin kullanıcısı başarıyla oluşturuldu: {adminUser.Email}");
                }
                else
                {
                    logger.LogInformation("Admin test kullanıcısı zaten mevcut: admin@etkinlikyonetimi.com");
                }
                
                // EventManager kullanıcısı
                var eventManagerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "EventManager" && r.TenantId == tenant.Id);
                if (eventManagerRole != null)
                {
                    bool eventManagerUserExists = await dbContext.Users.AnyAsync(u => u.Email == "manager@etkinlikyonetimi.com" && u.TenantId == tenant.Id);
                    
                    if (!eventManagerUserExists)
                    {
                        logger.LogInformation("EventManager test kullanıcısı oluşturuluyor: manager@etkinlikyonetimi.com");
                        
                        var eventManagerUser = new User
                        {
                            FirstName = "Etkinlik",
                            LastName = "Yöneticisi",
                            Email = "manager@etkinlikyonetimi.com",
                            PasswordHash = BC.HashPassword("123456"),
                            PhoneNumber = "5559876543",
                            TenantId = tenant.Id,
                            IsActive = true
                        };
                        
                        dbContext.Users.Add(eventManagerUser);
                        await dbContext.SaveChangesAsync();
                        
                        // EventManager rolünü ata
                        var userRole = new UserRole
                        {
                            UserId = eventManagerUser.Id,
                            RoleId = eventManagerRole.Id,
                            TenantId = tenant.Id
                        };
                        
                        dbContext.UserRoles.Add(userRole);
                        await dbContext.SaveChangesAsync();
                        
                        logger.LogInformation($"Test event manager kullanıcısı başarıyla oluşturuldu: {eventManagerUser.Email}");
                    }
                    else
                    {
                        logger.LogInformation("EventManager test kullanıcısı zaten mevcut: manager@etkinlikyonetimi.com");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    // Benzersizlik kısıtlaması hatası (duplicate key)
                    logger.LogWarning("Benzersizlik kısıtlaması hatası oluştu. Muhtemelen aynı e-posta adresine sahip kullanıcı zaten var: {Message}", sqlEx.Message);
                }
                else
                {
                    // Diğer DB güncelleme hataları
                    logger.LogError(ex, "Test kullanıcıları oluşturulurken veritabanı güncelleme hatası oluştu");
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Test kullanıcılarını oluştururken beklenmeyen bir hata oluştu");
                throw;
            }
        }
        
        private static async Task CreateDefaultRolesAsync(ApplicationDbContext dbContext, Tenant tenant, ILogger logger)
        {
            // Rolleri kontrol et
            var existingRoles = await dbContext.Roles.Where(r => r.TenantId == tenant.Id).ToListAsync();
            var rolesToCreate = new List<Role>();
            
            // Admin rolü
            if (!existingRoles.Any(r => r.Name == "Admin"))
            {
                rolesToCreate.Add(new Role { 
                    Name = "Admin", 
                    Description = "Sistem Yöneticisi", 
                    TenantId = tenant.Id 
                });
            }
            
            // EventManager rolü
            if (!existingRoles.Any(r => r.Name == "EventManager"))
            {
                rolesToCreate.Add(new Role { 
                    Name = "EventManager", 
                    Description = "Etkinlik Yöneticisi", 
                    TenantId = tenant.Id 
                });
            }
            
            // Attendee rolü
            if (!existingRoles.Any(r => r.Name == "Attendee"))
            {
                rolesToCreate.Add(new Role { 
                    Name = "Attendee", 
                    Description = "Etkinlik Katılımcısı", 
                    TenantId = tenant.Id 
                });
            }
            
            if (rolesToCreate.Any())
            {
                dbContext.Roles.AddRange(rolesToCreate);
                await dbContext.SaveChangesAsync();
                logger.LogInformation($"{rolesToCreate.Count} yeni rol oluşturuldu: {string.Join(", ", rolesToCreate.Select(r => r.Name))}");
            }
        }      
    }
} 