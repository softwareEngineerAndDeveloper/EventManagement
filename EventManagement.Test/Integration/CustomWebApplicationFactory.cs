using EventManagement.API;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventManagement.Test.Integration
{
    /// <summary>
    /// Entegrasyon testleri için özel web uygulama fabrikası
    /// Program sınıfı yerine ApiMarker kullanılarak erişim sorunları giderildi
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<ApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Test veritabanı olarak in-memory database kullan
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryEventTestDb");
                });

                // Test veritabanını oluştur ve test verileri ekle
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

                    db.Database.EnsureCreated();

                    try
                    {
                        // Test için örnek veriler
                        InitializeTestDatabase(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Test veritabanı oluşturulurken hata oluştu");
                    }
                }
            });
        }

        private void InitializeTestDatabase(ApplicationDbContext db)
        {
            // Tenant 1
            var tenant1 = new Tenant
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Test Tenant 1",
                Description = "Test Description 1",
                Subdomain = "test1",
                ContactEmail = "test1@example.com",
                ContactPhone = "5551234567",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            // Tenant 2
            var tenant2 = new Tenant
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Test Tenant 2",
                Description = "Test Description 2",
                Subdomain = "test2",
                ContactEmail = "test2@example.com",
                ContactPhone = "5559876543",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            db.Tenants.AddRange(tenant1, tenant2);

            // Admin Rolü
            var adminRole = new Role
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                Name = "Admin",
                TenantId = tenant1.Id,
                CreatedDate = DateTime.UtcNow
            };

            db.Roles.Add(adminRole);

            // Admin Kullanıcısı - Tenant 1
            var adminUser = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = "AQAAAAIAAYagAAAAELOLQZGOAZnVR5M3sBjkBN0t4v3+Rh+F/EHqDxkUUt2S+tVJbVh8uHBXpEUmSJ9iBw==", // P@ssword123
                PhoneNumber = "5551234567",
                TenantId = tenant1.Id,
                CreatedDate = DateTime.UtcNow
            };

            db.Users.Add(adminUser);

            // Admin user rolü
            var userRole = new UserRole
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                TenantId = tenant1.Id,
                CreatedDate = DateTime.UtcNow
            };

            db.UserRoles.Add(userRole);

            // Tenant 1 için örnek etkinlikler
            var event1 = new Event
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                Title = "Tenant 1 Event 1",
                Description = "Tenant 1 Description 1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Tenant 1 Location 1",
                MaxAttendees = 100,
                IsPublic = true,
                Status = EventStatus.Approved,
                CreatorId = adminUser.Id,
                TenantId = tenant1.Id,
                CreatedDate = DateTime.UtcNow
            };

            var event2 = new Event
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                Title = "Tenant 1 Event 2",
                Description = "Tenant 1 Description 2",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                Location = "Tenant 1 Location 2",
                MaxAttendees = 50,
                IsPublic = true,
                Status = EventStatus.Approved,
                CreatorId = adminUser.Id,
                TenantId = tenant1.Id,
                CreatedDate = DateTime.UtcNow
            };

            // Tenant 2 için örnek etkinlikler
            var event3 = new Event
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
                Title = "Tenant 2 Event 1",
                Description = "Tenant 2 Description 1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Tenant 2 Location 1",
                MaxAttendees = 75,
                IsPublic = true,
                Status = EventStatus.Approved,
                CreatorId = adminUser.Id,
                TenantId = tenant2.Id,
                CreatedDate = DateTime.UtcNow
            };

            db.Events.AddRange(event1, event2, event3);
            db.SaveChanges();
        }
    }
} 