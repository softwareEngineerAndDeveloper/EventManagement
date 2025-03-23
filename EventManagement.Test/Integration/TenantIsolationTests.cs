using EventManagement.Application.DTOs;
using EventManagement.Domain.Entities;
using EventManagement.Test.Integration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    /// <summary>
    /// Çoklu kiracı (multi-tenant) özelliğinin doğru çalıştığını test eden sınıf
    /// Tüm verilerin kiracılar arasında izole edildiğini doğrular
    /// </summary>
    public class TenantIsolationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Guid _tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public TenantIsolationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync(string email = "admin@test.com", string password = "P@ssword123", string subdomain = "test1")
        {
            // Admin kullanıcısı ile giriş yap
            var loginData = new LoginDto
            {
                Email = email,
                Password = password,
                Subdomain = subdomain // Default: Tenant 1'in subdomaini
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginData);
            loginResponse.EnsureSuccessStatusCode();

            var responseContent = await loginResponse.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ResponseDto<string>>(responseContent, _jsonOptions);

            return responseObject?.Data ?? "";
        }

        [Fact]
        public async Task TenantData_IsIsolatedBetweenTenants_Events()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - Tenant 1 etkinliklerini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response1 = await _client.GetAsync("/api/events");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();
            var result1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content1, _jsonOptions);

            // Act - Tenant 2 etkinliklerini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var response2 = await _client.GetAsync("/api/events");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();
            var result2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content2, _jsonOptions);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            
            // Tüm etkinliklerin doğru tenant'a ait olduğunu kontrol et
            foreach (var eventItem in result1.Data)
            {
                Assert.Equal(_tenant1Id, eventItem.TenantId);
            }
            
            foreach (var eventItem in result2.Data)
            {
                Assert.Equal(_tenant2Id, eventItem.TenantId);
            }
            
            // İki tenant'ın etkinliklerinin kesişmediğini kontrol et
            if (result1.Data.Count > 0 && result2.Data.Count > 0)
            {
                var tenant1EventIds = result1.Data.Select(e => e.Id).ToHashSet();
                var tenant2EventIds = result2.Data.Select(e => e.Id).ToHashSet();
                
                Assert.Empty(tenant1EventIds.Intersect(tenant2EventIds));
            }
        }

        [Fact]
        public async Task Tenant1CannotAccessTenant2Event_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Önce Tenant 2'nin bir etkinliğini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var eventsResponse = await _client.GetAsync("/api/events");
            eventsResponse.EnsureSuccessStatusCode();
            var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
            var eventsResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(eventsContent, _jsonOptions);
            
            // En az bir etkinlik olduğunu kontrol et
            Assert.NotNull(eventsResult);
            Assert.NotEmpty(eventsResult.Data);
            
            // Tenant 2'nin bir etkinliği
            var tenant2Event = eventsResult.Data.First();
            
            // Act - Tenant 1 ile Tenant 2'nin etkinliğine erişmeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response = await _client.GetAsync($"/api/events/{tenant2Event.Id}");
            
            // Assert - 404 Not Found yanıtı alınmalı
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_InTenant1_IsNotVisibleInTenant2()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            // Tenant 1'de yeni bir etkinlik oluştur
            var newEvent = new CreateEventDto
            {
                Title = "Tenant Isolation Test Event",
                Description = "Test for tenant isolation",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", newEvent);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            
            Assert.NotNull(createResult);
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Data);
            
            var createdEventId = createResult.Data.Id;
            
            // Act - Tenant 2 ile yeni oluşturulan etkinliğe erişmeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var getResponse = await _client.GetAsync($"/api/events/{createdEventId}");
            
            // Assert - 404 Not Found yanıtı alınmalı
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            
            // Temizlik - Oluşturulan etkinliği sil
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            await _client.DeleteAsync($"/api/events/{createdEventId}");
        }

        [Fact]
        public async Task TenantData_IsIsolatedBetweenTenants_Users()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - Tenant 1 kullanıcılarını al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response1 = await _client.GetAsync("/api/users");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();
            var result1 = JsonSerializer.Deserialize<ResponseDto<List<UserDto>>>(content1, _jsonOptions);

            // Act - Tenant 2 kullanıcılarını al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var response2 = await _client.GetAsync("/api/users");
            response2.EnsureSuccessStatusCode();
            var content2 = await response2.Content.ReadAsStringAsync();
            var result2 = JsonSerializer.Deserialize<ResponseDto<List<UserDto>>>(content2, _jsonOptions);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            
            // Tüm kullanıcıların doğru tenant'a ait olduğunu kontrol et
            foreach (var user in result1.Data)
            {
                Assert.Equal(_tenant1Id, user.TenantId);
            }
            
            foreach (var user in result2.Data)
            {
                Assert.Equal(_tenant2Id, user.TenantId);
            }
            
            // İki tenant'ın kullanıcılarının kesişmediğini kontrol et
            if (result1.Data.Count > 0 && result2.Data.Count > 0)
            {
                var tenant1UserIds = result1.Data.Select(u => u.Id).ToHashSet();
                var tenant2UserIds = result2.Data.Select(u => u.Id).ToHashSet();
                
                Assert.Empty(tenant1UserIds.Intersect(tenant2UserIds));
            }
        }

        [Fact]
        public async Task Tenant1CannotAddAttendeeToTenant2Event_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Önce Tenant 2'nin bir etkinliğini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var eventsResponse = await _client.GetAsync("/api/events");
            eventsResponse.EnsureSuccessStatusCode();
            var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
            var eventsResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(eventsContent, _jsonOptions);
            
            // En az bir etkinlik olduğunu kontrol et
            Assert.NotNull(eventsResult);
            Assert.NotEmpty(eventsResult.Data);
            
            // Tenant 2'nin bir etkinliği
            var tenant2Event = eventsResult.Data.First();
            
            // Tenant 2'nin etkinliğine eklenecek katılımcı
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = tenant2Event.Id,
                Name = "Test Katılımcı",
                Email = $"tenant-isolation-test-{Guid.NewGuid()}@example.com",
                Phone = "5551112233"
            };
            
            // Act - Tenant 1 ile Tenant 2'nin etkinliğine katılımcı eklemeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response = await _client.PostAsJsonAsync($"/api/events/{tenant2Event.Id}/attendees", createAttendeeDto);
            
            // Assert - 403 Forbidden yanıtı alınmalı
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TenantData_IsIsolatedBetweenTenants_Attendees()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Önce Tenant 1'de bir etkinlik oluştur ve katılımcı ekle
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            // Yeni etkinlik oluştur
            var newEvent = new CreateEventDto
            {
                Title = "Attendee Isolation Test Event",
                Description = "Test for attendee isolation",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createEventResponse = await _client.PostAsJsonAsync("/api/events", newEvent);
            createEventResponse.EnsureSuccessStatusCode();
            var createEventContent = await createEventResponse.Content.ReadAsStringAsync();
            var createEventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createEventContent, _jsonOptions);
            
            Assert.NotNull(createEventResult);
            Assert.True(createEventResult.IsSuccess);
            Assert.NotNull(createEventResult.Data);
            
            var eventId = createEventResult.Data.Id;
            
            // Etkinliğe katılımcı ekle
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Tenant1 Test Katılımcı",
                Email = $"tenant1-test-{Guid.NewGuid()}@example.com",
                Phone = "5551112233"
            };
            
            var createAttendeeResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto);
            createAttendeeResponse.EnsureSuccessStatusCode();
            
            // Act - Tenant 2 ile katılımcıları görmeye çalış
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var getAttendeesResponse = await _client.GetAsync($"/api/events/{eventId}/attendees");
            
            // Assert - 403 Forbidden yanıtı alınmalı
            Assert.Equal(HttpStatusCode.Forbidden, getAttendeesResponse.StatusCode);
            
            // Temizlik - Oluşturulan etkinliği sil
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task TenantData_IsIsolatedBetweenTenants_Roles()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Admin kullanıcının ID'sini al
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            
            // Act 1 - Tenant 1 ile admin kullanıcının rollerini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response1 = await _client.GetAsync($"/api/users/{adminUserId}/roles");
            response1.EnsureSuccessStatusCode();
            var content1 = await response1.Content.ReadAsStringAsync();
            var result1 = JsonSerializer.Deserialize<ResponseDto<List<Guid>>>(content1, _jsonOptions);
            
            // Assert 1 - Tenant 1'de admin rollerinin mevcut olduğunu kontrol et
            Assert.NotNull(result1);
            Assert.True(result1.IsSuccess);
            Assert.NotNull(result1.Data);
            Assert.NotEmpty(result1.Data);
            
            // Act 2 - Tenant 2 ile aynı kullanıcının rollerini görmeye çalış
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var response2 = await _client.GetAsync($"/api/users/{adminUserId}");
            
            // Assert 2 - 404 Not Found yanıtı alınmalı çünkü kullanıcı Tenant 2'de bulunmuyor
            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
        }

        [Fact]
        public async Task Tenant1CannotUpdateTenant2Data_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Önce Tenant 2'nin bir etkinliğini al
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var eventsResponse = await _client.GetAsync("/api/events");
            eventsResponse.EnsureSuccessStatusCode();
            var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
            var eventsResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(eventsContent, _jsonOptions);
            
            // En az bir etkinlik olduğunu kontrol et
            Assert.NotNull(eventsResult);
            Assert.NotEmpty(eventsResult.Data);
            
            // Tenant 2'nin bir etkinliği
            var tenant2Event = eventsResult.Data.First();
            
            // Tenant 2'nin etkinliğini güncellemek için DTO
            var updateEventDto = new UpdateEventDto
            {
                Title = "Updated by Tenant 1",
                Description = "This should fail due to tenant isolation",
                Location = "Somewhere else",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                MaxAttendees = 200,
                IsPublic = false
            };
            
            // Act - Tenant 1 ile Tenant 2'nin etkinliğini güncellemeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response = await _client.PutAsJsonAsync($"/api/events/{tenant2Event.Id}", updateEventDto);
            
            // Assert - 404 Not Found yanıtı alınmalı çünkü Tenant 1 bu etkinliği göremez
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TenantCrossOver_UserCannotAccessOtherTenantWithSameCredentials()
        {
            // Arrange - Tenant 1 için token al
            var token1 = await GetAuthTokenAsync(email: "admin@test.com", password: "P@ssword123", subdomain: "test1");
            
            // Arrange - Aynı kullanıcı bilgileriyle Tenant 2'ye giriş yapmaya çalış
            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            
            // Act 1 - Tenant 1 ile etkinlikleri görebilmeli
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var response1 = await _client.GetAsync("/api/events");
            
            // Assert 1 - Başarılı yanıt alınmalı
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            
            // Act 2 - Tenant 2 ile aynı token'ı kullanarak başka bir etkinlik oluşturmaya çalış
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            
            var newEvent = new CreateEventDto
            {
                Title = "Cross-Tenant Attack Test",
                Description = "This should fail due to tenant isolation",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant2Id, // Tenant 2'ye ait bir etkinlik oluşturmaya çalışıyoruz
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID (Tenant 1'in kullanıcısı)
            };
            
            var response2 = await _client.PostAsJsonAsync("/api/events", newEvent);
            
            // Assert 2 - 400 veya 403 gibi bir hata yanıtı alınmalı
            Assert.True(response2.StatusCode == HttpStatusCode.BadRequest || 
                        response2.StatusCode == HttpStatusCode.Forbidden || 
                        response2.StatusCode == HttpStatusCode.Unauthorized);
        }
    }
} 