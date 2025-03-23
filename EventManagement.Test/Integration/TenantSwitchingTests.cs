using EventManagement.Application.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    /// <summary>
    /// Tenant'lar arasında geçiş yaparken sistemin güvenlik ve izolasyon kurallarını test eden sınıf.
    /// </summary>
    public class TenantSwitchingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Guid _tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public TenantSwitchingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync(string email = "admin@test.com", string password = "P@ssword123", string subdomain = "test1")
        {
            var loginData = new LoginDto
            {
                Email = email,
                Password = password,
                Subdomain = subdomain
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginData);
            loginResponse.EnsureSuccessStatusCode();

            var responseContent = await loginResponse.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ResponseDto<string>>(responseContent, _jsonOptions);

            return responseObject?.Data ?? "";
        }

        [Fact]
        public async Task SwitchingTenants_MaintainsIsolation_ForEvents()
        {
            // Arrange - Tenant 1 için token al
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Tenant 1'de test etkinliği oluştur
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            var tenant1Event = new CreateEventDto
            {
                Title = "Tenant Switching Test Event 1",
                Description = "Test for tenant isolation when switching tenants",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location 1",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createResponse1 = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse1.EnsureSuccessStatusCode();
            var createContent1 = await createResponse1.Content.ReadAsStringAsync();
            var createResult1 = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent1, _jsonOptions);
            var tenant1EventId = createResult1.Data.Id;
            
            // Tenant 2'ye geçiş yap
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            
            // Act 1 - Tenant 2'de iken Tenant 1'in etkinliğine erişmeyi dene
            var getResponse = await _client.GetAsync($"/api/events/{tenant1EventId}");
            
            // Assert 1 - 404 Not Found yanıtı alınmalı çünkü Tenant 2 bu etkinliği göremez
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            
            // Act 2 - Tenant 2'de iken yeni bir etkinlik oluştur
            var tenant2Event = new CreateEventDto
            {
                Title = "Tenant Switching Test Event 2",
                Description = "Test for tenant isolation when switching tenants",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location 2",
                MaxAttendees = 100,
                IsPublic = true,
                TenantId = _tenant2Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createResponse2 = await _client.PostAsJsonAsync("/api/events", tenant2Event);
            
            // Assert 2 - Başarısız olmalı (400 veya 403) çünkü Tenant 1'in kullanıcısı Tenant 2'ye etkinlik ekleyemez
            Assert.True(createResponse2.StatusCode == HttpStatusCode.BadRequest || 
                        createResponse2.StatusCode == HttpStatusCode.Forbidden ||
                        createResponse2.StatusCode == HttpStatusCode.Unauthorized);
            
            // Tenant 1'e geri dön
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            // Act 3 - Tenant 1'in etkinliklerini al
            var eventsResponse = await _client.GetAsync("/api/events");
            eventsResponse.EnsureSuccessStatusCode();
            var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
            var eventsResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(eventsContent, _jsonOptions);
            
            // Assert 3 - Yalnızca Tenant 1'in etkinliklerini içermeli
            Assert.NotNull(eventsResult);
            Assert.True(eventsResult.IsSuccess);
            Assert.All(eventsResult.Data, e => Assert.Equal(_tenant1Id, e.TenantId));
            
            // Temizlik - Oluşturulan etkinliği sil
            await _client.DeleteAsync($"/api/events/{tenant1EventId}");
        }

        [Fact]
        public async Task RapidTenantSwitching_MaintainsIsolation_ForAttendees()
        {
            // Arrange - Tenant 1 için token al
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Tenant 1'de test etkinliği oluştur
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            var tenant1Event = new CreateEventDto
            {
                Title = "Rapid Tenant Switching Test",
                Description = "Test for attendee isolation when rapidly switching tenants",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createEventResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createEventResponse.EnsureSuccessStatusCode();
            var createEventContent = await createEventResponse.Content.ReadAsStringAsync();
            var createEventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createEventContent, _jsonOptions);
            var eventId = createEventResult.Data.Id;
            
            // Etkinliğe katılımcı ekle
            var attendee1 = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Tenant1 Katılımcı",
                Email = $"tenant1-attendee-{Guid.NewGuid()}@example.com",
                Phone = "5551112233"
            };
            
            var createAttendeeResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", attendee1);
            createAttendeeResponse.EnsureSuccessStatusCode();
            var createAttendeeContent = await createAttendeeResponse.Content.ReadAsStringAsync();
            var createAttendeeResult = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(createAttendeeContent, _jsonOptions);
            var attendeeId = createAttendeeResult.Data.Id;
            
            // Act 1 - Tenant 2'ye hızlı geçiş yap ve katılımcıyı görmeye çalış
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            var attendeeResponse = await _client.GetAsync($"/api/events/attendees/{attendeeId}");
            
            // Assert 1 - 404 Not Found yanıtı alınmalı
            Assert.Equal(HttpStatusCode.NotFound, attendeeResponse.StatusCode);
            
            // Act 2 - Hızlıca Tenant 1'e geri dön ve katılımcıyı görmeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            var attendeeResponse2 = await _client.GetAsync($"/api/events/attendees/{attendeeId}");
            
            // Assert 2 - Başarılı yanıt alınmalı
            attendeeResponse2.EnsureSuccessStatusCode();
            var attendeeContent2 = await attendeeResponse2.Content.ReadAsStringAsync();
            var attendeeResult2 = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(attendeeContent2, _jsonOptions);
            
            Assert.NotNull(attendeeResult2);
            Assert.True(attendeeResult2.IsSuccess);
            Assert.Equal(attendeeId, attendeeResult2.Data.Id);
            
            // Temizlik - Oluşturulan etkinliği sil
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task HeaderTampering_CannotBypassTenantIsolation()
        {
            // Arrange - Tenant 1 için token al
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Önce Tenant 1'de bir etkinlik oluştur
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            var tenant1Event = new CreateEventDto
            {
                Title = "Header Tampering Test Event",
                Description = "Test for protection against header tampering",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000011") // Admin user ID
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            var eventId = createResult.Data.Id;
            
            // Act 1 - Tenant 2 header'ı ile bir katılımcı ekleyelim, ama DTO'da Tenant 1'in etkinliğini kullanmaya çalışalım
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            
            var attendee = new CreateAttendeeDto
            {
                EventId = eventId, // Tenant 1'in etkinliği
                Name = "Header Tampering Test",
                Email = $"tampering-test-{Guid.NewGuid()}@example.com",
                Phone = "5559876543"
            };
            
            var attendeeResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", attendee);
            
            // Assert 1 - 403 Forbidden veya 404 Not Found yanıtı alınmalı
            Assert.True(attendeeResponse.StatusCode == HttpStatusCode.Forbidden || 
                        attendeeResponse.StatusCode == HttpStatusCode.NotFound);
            
            // Act 2 - Özel bir header eklemeyi dene
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            _client.DefaultRequestHeaders.Add("X-Override-Tenant-Isolation", "true"); // Böyle bir header gerçekte olmamalı
            
            var getResponse = await _client.GetAsync($"/api/events/{eventId}");
            
            // Assert 2 - Yine de 404 Not Found yanıtı alınmalı
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            
            // Temizlik - Oluşturulan etkinliği sil
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Remove("X-Override-Tenant-Isolation");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            await _client.DeleteAsync($"/api/events/{eventId}");
        }
    }
} 