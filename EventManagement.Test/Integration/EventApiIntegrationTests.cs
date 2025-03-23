using EventManagement.API.Controllers;
using EventManagement.Application.DTOs;
using EventManagement.Domain.Entities;
using EventManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    // WebApplicationFactory<ApiMarker> kullanımı ile API projesine erişim sağlanır
    // Bu sınıf integration testing için kullanılır
    public class EventApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public EventApiIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            // Kullanıcı girişi ve JWT token alımı
            var loginData = new LoginDto
            {
                Email = "admin@test.com",
                Password = "P@ssword123",
                Subdomain = "test1" // Tenant 1'in subdomaini
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginData);
            loginResponse.EnsureSuccessStatusCode();

            var responseContent = await loginResponse.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ResponseDto<string>>(responseContent, _jsonOptions);

            return responseObject?.Data ?? "";
        }

        [Fact]
        public async Task GetAllEvents_ReturnsEventsForTenant()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");

            // Act
            var response = await _client.GetAsync("/api/events");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Count > 0);

            // Tüm etkinliklerin aynı tenant'a ait olup olmadığını kontrol et
            foreach (var eventItem in result.Data)
            {
                Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), eventItem.TenantId);
            }
        }

        [Fact]
        public async Task GetAllEvents_WithDifferentTenant_ReturnsOnlyTenantEvents()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Tenant 1'in etkinliklerini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            var response1 = await _client.GetAsync("/api/events");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();
            var result1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content1, _jsonOptions);
            
            // Tenant 2'nin etkinliklerini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000002");
            var response2 = await _client.GetAsync("/api/events");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();
            var result2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content2, _jsonOptions);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            
            // İki tenant'ın etkinliklerinin farklı olduğunu doğrula
            if (result1.Data.Count > 0 && result2.Data.Count > 0)
            {
                var tenant1EventIds = result1.Data.Select(e => e.Id).ToHashSet();
                var tenant2EventIds = result2.Data.Select(e => e.Id).ToHashSet();
                
                Assert.Empty(tenant1EventIds.Intersect(tenant2EventIds));
            }
        }

        [Fact]
        public async Task CreateEvent_AddsEventToCorrectTenant()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");

            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var newEvent = new CreateEventDto
            {
                Title = "Integration Test Event",
                Description = "Test Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = tenantId,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/events", newEvent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<EventDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Integration Test Event", result.Data.Title);
            Assert.Equal(tenantId, result.Data.TenantId);

            // Verify the new event is only visible to the correct tenant
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000002");
            var getResponse = await _client.GetAsync($"/api/events/{result.Data.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
} 