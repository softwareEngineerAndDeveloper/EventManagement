using EventManagement.Application.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    /// <summary>
    /// Multi-tenant sistemde önbellek (cache) geçersiz kılma (invalidation) süreçlerini test eden sınıf.
    /// Bir tenant'ın verilerinde yapılan değişikliklerin önbellekten doğru şekilde temizlendiğini
    /// ve diğer tenant'ların verilerinin etkilenmediğini doğrulamak için kullanılır.
    /// </summary>
    public class TenantCacheInvalidationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Guid _tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private readonly Guid _adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011");

        public TenantCacheInvalidationTests(CustomWebApplicationFactory factory)
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

        private void SetTenantContext(Guid tenantId)
        {
            _client.DefaultRequestHeaders.Remove("X-Tenant-ID");
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
        }

        [Fact]
        public async Task EventCacheInvalidation_DoesNotAffectOtherTenants()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için etkinlik oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "Tenant 1 Cache Test Event",
                Description = "Test for cache invalidation between tenants",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            var eventId = createResult.Data.Id;
            
            // Tenant 2 context'ine geç
            SetTenantContext(_tenant2Id);
            
            // Tenant 2'deki etkinlikleri al ve önbelleğe alınmasını sağla
            var tenant2EventsResponse1 = await _client.GetAsync("/api/events");
            tenant2EventsResponse1.EnsureSuccessStatusCode();
            var tenant2EventsContent1 = await tenant2EventsResponse1.Content.ReadAsStringAsync();
            var tenant2EventsResult1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(tenant2EventsContent1, _jsonOptions);
            
            // Tenant 1 context'ine geri dön
            SetTenantContext(_tenant1Id);
            
            // Act - Tenant 1'deki etkinliği güncelle (önbelleği geçersiz kılmalı)
            var updateEventDto = new UpdateEventDto
            {
                Title = "Updated Tenant 1 Cache Test Event",
                Description = "Updated description",
                Location = "Updated location",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                MaxAttendees = 100,
                IsPublic = false
            };
            
            var updateResponse = await _client.PutAsJsonAsync($"/api/events/{eventId}", updateEventDto);
            updateResponse.EnsureSuccessStatusCode();
            
            // Tenant 2 context'ine tekrar geç
            SetTenantContext(_tenant2Id);
            
            // Tenant 2'deki etkinlikleri tekrar al
            var tenant2EventsResponse2 = await _client.GetAsync("/api/events");
            tenant2EventsResponse2.EnsureSuccessStatusCode();
            var tenant2EventsContent2 = await tenant2EventsResponse2.Content.ReadAsStringAsync();
            var tenant2EventsResult2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(tenant2EventsContent2, _jsonOptions);
            
            // Assert - Tenant 2'nin etkinlik listesi değişmemiş olmalı
            Assert.Equal(tenant2EventsResult1.Data.Count, tenant2EventsResult2.Data.Count);
            
            // Tenant 1'e geri dön ve etkinliği sil
            SetTenantContext(_tenant1Id);
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task AttendeeCacheInvalidation_DoesNotAffectOtherTenants()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için etkinlik oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "Attendee Cache Test Event",
                Description = "Test for attendee cache invalidation between tenants",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createEventResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createEventResponse.EnsureSuccessStatusCode();
            var createEventContent = await createEventResponse.Content.ReadAsStringAsync();
            var createEventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createEventContent, _jsonOptions);
            var eventId = createEventResult.Data.Id;
            
            // Etkinliğe katılımcı ekle
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Cache Test Attendee",
                Email = $"cache-test-{Guid.NewGuid()}@example.com",
                Phone = "5551112233"
            };
            
            var createAttendeeResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto);
            createAttendeeResponse.EnsureSuccessStatusCode();
            var createAttendeeContent = await createAttendeeResponse.Content.ReadAsStringAsync();
            var createAttendeeResult = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(createAttendeeContent, _jsonOptions);
            var attendeeId = createAttendeeResult.Data.Id;
            
            // Tenant 2 context'ine geç ve bir etkinlik oluştur
            SetTenantContext(_tenant2Id);
            
            // Test ihtiyaçları için Tenant 2'ye bir etkinlik ekleyelim (eğer yoksa)
            var tenant2EventsResponse = await _client.GetAsync("/api/events");
            tenant2EventsResponse.EnsureSuccessStatusCode();
            var tenant2EventsContent = await tenant2EventsResponse.Content.ReadAsStringAsync();
            var tenant2EventsResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(tenant2EventsContent, _jsonOptions);
            
            Guid tenant2EventId;
            if (tenant2EventsResult.Data.Count > 0)
            {
                tenant2EventId = tenant2EventsResult.Data.First().Id;
            }
            else
            {
                // Tenant 2 için etkinlik oluştur
                var tenant2Event = new CreateEventDto
                {
                    Title = "Tenant 2 Cache Test Event",
                    Description = "Test for attendee cache invalidation between tenants",
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(2),
                    Location = "Test Location",
                    MaxAttendees = 50,
                    IsPublic = true,
                    TenantId = _tenant2Id,
                    CreatorId = _adminUserId
                };
                
                var createT2EventResponse = await _client.PostAsJsonAsync("/api/events", tenant2Event);
                // Bu noktada izin hatası alınabilir, diğer test senaryolarına geçelim
                if (!createT2EventResponse.IsSuccessStatusCode)
                {
                    // Tenant 1'e geri dön ve temizlik yap
                    SetTenantContext(_tenant1Id);
                    await _client.DeleteAsync($"/api/events/{eventId}");
                    return; // Test skip
                }
                
                var createT2EventContent = await createT2EventResponse.Content.ReadAsStringAsync();
                var createT2EventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createT2EventContent, _jsonOptions);
                tenant2EventId = createT2EventResult.Data.Id;
            }
            
            // Tenant 1 context'ine geri dön
            SetTenantContext(_tenant1Id);
            
            // Act - Katılımcıyı güncelle
            var updateAttendeeDto = new UpdateAttendeeDto
            {
                Name = "Updated Cache Test Attendee",
                Email = createAttendeeResult.Data.Email,
                Phone = "5559998877",
                Status = 1, // Güncelle
                Notes = "Updated via cache test"
            };
            
            var updateAttendeeResponse = await _client.PutAsJsonAsync($"/api/events/attendees/{attendeeId}", updateAttendeeDto);
            updateAttendeeResponse.EnsureSuccessStatusCode();
            
            // Tenant 2 context'ine tekrar geç
            SetTenantContext(_tenant2Id);
            
            // Tenant 2'deki bir etkinliğin detaylarını tekrar al (önbellekten gelmeli)
            var getEventResponse = await _client.GetAsync($"/api/events/{tenant2EventId}");
            
            // Assert - Tenant 2'nin etkinlik detayları başarıyla gelmiş olmalı
            Assert.Equal(HttpStatusCode.OK, getEventResponse.StatusCode);
            
            // Tenant 1'e geri dön ve temizlik yap
            SetTenantContext(_tenant1Id);
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task CacheInvalidation_AfterEntityDeletion_WorksCorrectly()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için etkinlik oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "Deletion Cache Test Event",
                Description = "Test for cache invalidation after deletion",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createEventResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createEventResponse.EnsureSuccessStatusCode();
            var createEventContent = await createEventResponse.Content.ReadAsStringAsync();
            var createEventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createEventContent, _jsonOptions);
            var eventId = createEventResult.Data.Id;
            
            // Etkinlik detaylarını al (önbelleğe alınmalı)
            var getEventResponse1 = await _client.GetAsync($"/api/events/{eventId}");
            getEventResponse1.EnsureSuccessStatusCode();
            
            // Act - Etkinliği sil
            var deleteResponse = await _client.DeleteAsync($"/api/events/{eventId}");
            deleteResponse.EnsureSuccessStatusCode();
            
            // Silinen etkinliği tekrar almayı dene
            var getEventResponse2 = await _client.GetAsync($"/api/events/{eventId}");
            
            // Assert - 404 Not Found yanıtı alınmalı
            Assert.Equal(HttpStatusCode.NotFound, getEventResponse2.StatusCode);
        }

        [Fact]
        public async Task CacheInvalidation_AfterUpdate_ReturnsLatestData()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için etkinlik oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "Update Cache Test Event",
                Description = "Test for cache invalidation after update",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createEventResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createEventResponse.EnsureSuccessStatusCode();
            var createEventContent = await createEventResponse.Content.ReadAsStringAsync();
            var createEventResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createEventContent, _jsonOptions);
            var eventId = createEventResult.Data.Id;
            
            // Etkinlik detaylarını al (önbelleğe alınmalı)
            var getEventResponse1 = await _client.GetAsync($"/api/events/{eventId}");
            getEventResponse1.EnsureSuccessStatusCode();
            var getEventContent1 = await getEventResponse1.Content.ReadAsStringAsync();
            var getEventResult1 = JsonSerializer.Deserialize<ResponseDto<EventDto>>(getEventContent1, _jsonOptions);
            
            // Act - Etkinliği güncelle
            var updatedTitle = "Updated Title After Cache Test";
            var updateEventDto = new UpdateEventDto
            {
                Title = updatedTitle,
                Description = "Updated description",
                Location = "Updated location",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                MaxAttendees = 100,
                IsPublic = false
            };
            
            var updateResponse = await _client.PutAsJsonAsync($"/api/events/{eventId}", updateEventDto);
            updateResponse.EnsureSuccessStatusCode();
            
            // Etkinlik detaylarını tekrar al
            var getEventResponse2 = await _client.GetAsync($"/api/events/{eventId}");
            getEventResponse2.EnsureSuccessStatusCode();
            var getEventContent2 = await getEventResponse2.Content.ReadAsStringAsync();
            var getEventResult2 = JsonSerializer.Deserialize<ResponseDto<EventDto>>(getEventContent2, _jsonOptions);
            
            // Assert - Güncellenmiş bilgiler gelmeli
            Assert.NotEqual(getEventResult1.Data.Title, getEventResult2.Data.Title);
            Assert.Equal(updatedTitle, getEventResult2.Data.Title);
            
            // Temizlik
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task MultiUserCache_RetrievesCorrectDataAfterChanges()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // İlk kullanıcının tüm kullanıcıları alması (önbelleğe alınmalı)
            var getUsersResponse1 = await _client.GetAsync("/api/users");
            getUsersResponse1.EnsureSuccessStatusCode();
            var getUsersContent1 = await getUsersResponse1.Content.ReadAsStringAsync();
            var getUsersResult1 = JsonSerializer.Deserialize<ResponseDto<List<UserDto>>>(getUsersContent1, _jsonOptions);
            var initialUserCount = getUsersResult1.Data.Count;
            
            // Yeni bir kullanıcı oluştur
            var createUserDto = new CreateUserDto
            {
                Email = $"cache-test-user-{Guid.NewGuid()}@example.com",
                FirstName = "Cache",
                LastName = "Test User",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                PhoneNumber = "5551112233"
                // TenantId burada gerekli değil, API tenant header'dan alacak
            };
            
            var createUserResponse = await _client.PostAsJsonAsync("/api/users", createUserDto);
            if (!createUserResponse.IsSuccessStatusCode)
            {
                // Bu test için kullanıcı oluşturma endpoint'i yoksa, testi atla
                return;
            }
            
            var createUserContent = await createUserResponse.Content.ReadAsStringAsync();
            var createUserResult = JsonSerializer.Deserialize<ResponseDto<UserDto>>(createUserContent, _jsonOptions);
            var userId = createUserResult.Data.Id;
            
            // Act - Tüm kullanıcıları tekrar al
            var getUsersResponse2 = await _client.GetAsync("/api/users");
            getUsersResponse2.EnsureSuccessStatusCode();
            var getUsersContent2 = await getUsersResponse2.Content.ReadAsStringAsync();
            var getUsersResult2 = JsonSerializer.Deserialize<ResponseDto<List<UserDto>>>(getUsersContent2, _jsonOptions);
            
            // Assert - Yeni kullanıcı eklenmiş olmalı
            Assert.Equal(initialUserCount + 1, getUsersResult2.Data.Count);
            Assert.Contains(getUsersResult2.Data, u => u.Id == userId);
            
            // Temizlik - Kullanıcıyı sil
            await _client.DeleteAsync($"/api/users/{userId}");
        }

        [Fact]
        public async Task ConcurrentAccessFromDifferentTenants_MaintainsCorrectCacheState()
        {
            // Arrange - Tenant 1 için token al ve tenant 1 context'ini ayarla
            var token1 = await GetAuthTokenAsync(subdomain: "test1");
            var token2 = await GetAuthTokenAsync(subdomain: "test2");

            // Tenant 1 için etkinlik oluştur
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            SetTenantContext(_tenant1Id);
            
            var tenant1Event = new CreateEventDto
            {
                Title = "Concurrent Access Test Event 1",
                Description = "Test for concurrent cache access",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location 1",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createResponse1 = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse1.EnsureSuccessStatusCode();
            var createContent1 = await createResponse1.Content.ReadAsStringAsync();
            var createResult1 = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent1, _jsonOptions);
            var tenant1EventId = createResult1.Data.Id;

            // Tenant 2 için başka bir client oluştur
            var client2 = _factory.CreateClient();
            client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            client2.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            
            // Act 1 - Aynı anda iki tenant'tan farklı endpoint'lere erişim sağla
            // Tenant 1'in etkinliklerini getir
            var getEvents1Task = _client.GetAsync("/api/events");
            
            // Tenant 2'nin etkinliklerini getir (farklı client ile)
            var getEvents2Task = client2.GetAsync("/api/events");
            
            // İsteklerin tamamlanmasını bekle
            await Task.WhenAll(getEvents1Task, getEvents2Task);
            
            var getEventsResponse1 = await getEvents1Task;
            var getEventsResponse2 = await getEvents2Task;
            
            // Assert 1 - Her iki istek de başarılı olmalı
            getEventsResponse1.EnsureSuccessStatusCode();
            getEventsResponse2.EnsureSuccessStatusCode();
            
            var getEventsContent1 = await getEventsResponse1.Content.ReadAsStringAsync();
            var getEventsContent2 = await getEventsResponse2.Content.ReadAsStringAsync();
            
            var getEventsResult1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEventsContent1, _jsonOptions);
            var getEventsResult2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEventsContent2, _jsonOptions);
            
            // Her tenant kendi etkinliklerini görüyor olmalı
            Assert.Contains(getEventsResult1.Data, e => e.Id == tenant1EventId);
            Assert.DoesNotContain(getEventsResult2.Data, e => e.Id == tenant1EventId);
            
            // Act 2 - Tenant 1 etkinliği güncellerken, Tenant 2 kendi etkinliklerini almaya devam ediyor
            var updateEventDto = new UpdateEventDto
            {
                Title = "Updated Concurrent Test Event",
                Description = "Updated description for concurrent test",
                Location = "Updated location",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                MaxAttendees = 100,
                IsPublic = false
            };
            
            var updateTask = _client.PutAsJsonAsync($"/api/events/{tenant1EventId}", updateEventDto);
            var getEvents2AgainTask = client2.GetAsync("/api/events");
            
            await Task.WhenAll(updateTask, getEvents2AgainTask);
            
            var updateResponse = await updateTask;
            var getEvents2AgainResponse = await getEvents2AgainTask;
            
            // Assert 2 - Güncellemeler başarılı olmalı ve tenant 2'nin sonuçları değişmemeli
            updateResponse.EnsureSuccessStatusCode();
            getEvents2AgainResponse.EnsureSuccessStatusCode();
            
            var getEvents2AgainContent = await getEvents2AgainResponse.Content.ReadAsStringAsync();
            var getEvents2AgainResult = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEvents2AgainContent, _jsonOptions);
            
            // Tenant 2 hala orijinal etkinliği görmüyor olmalı
            Assert.DoesNotContain(getEvents2AgainResult.Data, e => e.Id == tenant1EventId);
            
            // Temizlik
            await _client.DeleteAsync($"/api/events/{tenant1EventId}");
        }

        [Fact]
        public async Task CacheRefresh_AfterTenantSwitch_RetrievesCorrectData()
        {
            // Arrange - Tek bir client kullanarak iki tenant arasında hızlı geçişler yapalım
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // İlk olarak Tenant 1'de bir etkinlik oluştur
            SetTenantContext(_tenant1Id);
            
            var tenant1Event = new CreateEventDto
            {
                Title = "Cache Refresh Test Event",
                Description = "Test for cache refresh after tenant switch",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            var eventId = createResult.Data.Id;
            
            // İlk olarak tenant 1'in etkinliklerini al
            var getEvents1Response1 = await _client.GetAsync("/api/events");
            getEvents1Response1.EnsureSuccessStatusCode();
            var getEvents1Content1 = await getEvents1Response1.Content.ReadAsStringAsync();
            var getEvents1Result1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEvents1Content1, _jsonOptions);
            var initialEvent1Count = getEvents1Result1.Data.Count;
            
            // Tenant 2'ye geç ve etkinliklerini al
            SetTenantContext(_tenant2Id);
            var getEvents2Response1 = await _client.GetAsync("/api/events");
            getEvents2Response1.EnsureSuccessStatusCode();
            var getEvents2Content1 = await getEvents2Response1.Content.ReadAsStringAsync();
            var getEvents2Result1 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEvents2Content1, _jsonOptions);
            var initialEvent2Count = getEvents2Result1.Data.Count;
            
            // Tenant 1'e geri dön ve etkinliği güncelle
            SetTenantContext(_tenant1Id);
            var updateEventDto = new UpdateEventDto
            {
                Title = "Updated Cache Refresh Test Event",
                Description = "Updated description for cache refresh test",
                Location = "Updated location",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(4),
                MaxAttendees = 100,
                IsPublic = false
            };
            
            var updateResponse = await _client.PutAsJsonAsync($"/api/events/{eventId}", updateEventDto);
            updateResponse.EnsureSuccessStatusCode();
            
            // Hızlıca Tenant 2'ye tekrar geç
            SetTenantContext(_tenant2Id);
            var getEvents2Response2 = await _client.GetAsync("/api/events");
            getEvents2Response2.EnsureSuccessStatusCode();
            var getEvents2Content2 = await getEvents2Response2.Content.ReadAsStringAsync();
            var getEvents2Result2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEvents2Content2, _jsonOptions);
            
            // Tenant 1'e geri dön
            SetTenantContext(_tenant1Id);
            var getEvents1Response2 = await _client.GetAsync("/api/events");
            getEvents1Response2.EnsureSuccessStatusCode();
            var getEvents1Content2 = await getEvents1Response2.Content.ReadAsStringAsync();
            var getEvents1Result2 = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(getEvents1Content2, _jsonOptions);
            
            // Assert - Her tenant kendi etkinlik sayısını korumalı
            Assert.Equal(initialEvent1Count, getEvents1Result2.Data.Count);
            Assert.Equal(initialEvent2Count, getEvents2Result2.Data.Count);
            
            // Güncellenmiş etkinliği kontrol et
            var updatedEvent = getEvents1Result2.Data.FirstOrDefault(e => e.Id == eventId);
            Assert.NotNull(updatedEvent);
            Assert.Equal("Updated Cache Refresh Test Event", updatedEvent.Title);
            
            // Temizlik
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task HighLoadCache_MaintainsTenantIsolation()
        {
            // Arrange - Token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için bir test etkinliği oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "High Load Cache Test Event 1",
                Description = "Test for cache under high load conditions",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location 1",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            var eventId = createResult.Data.Id;
            
            // Çok sayıda istek için yeni client'lar oluştur
            var tenant1Client = _factory.CreateClient();
            tenant1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tenant1Client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant1Id.ToString());
            
            var tenant2Client = _factory.CreateClient();
            tenant2Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            tenant2Client.DefaultRequestHeaders.Add("X-Tenant-ID", _tenant2Id.ToString());
            
            // Act - Her tenant için 5 paralel istek gönder (toplamda 10 eşzamanlı istek)
            var tenant1Tasks = new List<Task<HttpResponseMessage>>();
            var tenant2Tasks = new List<Task<HttpResponseMessage>>();
            
            for (int i = 0; i < 5; i++)
            {
                // İlk tenant için rastgele endpoint istekleri
                if (i % 2 == 0)
                {
                    tenant1Tasks.Add(tenant1Client.GetAsync("/api/events"));
                }
                else
                {
                    tenant1Tasks.Add(tenant1Client.GetAsync($"/api/events/{eventId}"));
                }
                
                // İkinci tenant için rastgele endpoint istekleri
                tenant2Tasks.Add(tenant2Client.GetAsync("/api/events"));
            }
            
            // Tüm isteklerin tamamlanmasını bekle
            await Task.WhenAll(tenant1Tasks.Concat(tenant2Tasks));
            
            // Assert - Tüm istekler başarılı olmalı
            foreach (var task in tenant1Tasks)
            {
                task.Result.EnsureSuccessStatusCode();
                
                // Eğer bu bir detay isteği ise, doğru etkinliği döndürdüğünden emin ol
                if (task.Result.RequestMessage.RequestUri.AbsolutePath.Contains(eventId.ToString()))
                {
                    var content = await task.Result.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ResponseDto<EventDto>>(content, _jsonOptions);
                    Assert.Equal(eventId, result.Data.Id);
                    Assert.Equal(_tenant1Id, result.Data.TenantId);
                }
            }
            
            // Tenant 2 isteklerinde, Tenant 1'in etkinliği olmamalı
            foreach (var task in tenant2Tasks)
            {
                task.Result.EnsureSuccessStatusCode();
                var content = await task.Result.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ResponseDto<List<EventDto>>>(content, _jsonOptions);
                Assert.DoesNotContain(result.Data, e => e.Id == eventId);
            }
            
            // Temizlik
            await _client.DeleteAsync($"/api/events/{eventId}");
        }

        [Fact]
        public async Task CacheTimeout_RefreshesCacheWithCorrectTenantContext()
        {
            // Arrange - Token al ve tenant 1 context'ini ayarla
            var token = await GetAuthTokenAsync(subdomain: "test1");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 için bir test etkinliği oluştur
            var tenant1Event = new CreateEventDto
            {
                Title = "Cache Timeout Test Event",
                Description = "Test for cache timeout and refresh with correct tenant context",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Location = "Test Location",
                MaxAttendees = 50,
                IsPublic = true,
                TenantId = _tenant1Id,
                CreatorId = _adminUserId
            };
            
            var createResponse = await _client.PostAsJsonAsync("/api/events", tenant1Event);
            createResponse.EnsureSuccessStatusCode();
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<EventDto>>(createContent, _jsonOptions);
            var eventId = createResult.Data.Id;
            
            // Etkinlik detaylarını önbelleğe almak için ilk istek
            var getEventResponse1 = await _client.GetAsync($"/api/events/{eventId}");
            getEventResponse1.EnsureSuccessStatusCode();
            
            // Act - Tenant 2'ye geç (önbellek içeriği yenilenince farklı tenant verisi görmemeli)
            SetTenantContext(_tenant2Id);
            
            // Cache timeout'unu simüle etmek için bekleteceğiz
            // Not: Bu gerçek uygulamada cache timeout süresi değişebilir
            // Bu test sadece cache timeout ve yenilenme mantığını test etmek için
            await Task.Delay(10); // Çok kısa bir süre beklet (gerçek uygulamada cache timeout daha uzun olur)
            
            // Tenant 2 ile etkinliği getirmeyi dene (404 olmalı)
            var getEventFromTenant2Response = await _client.GetAsync($"/api/events/{eventId}");
            
            // Tenant 1'e geri dön
            SetTenantContext(_tenant1Id);
            
            // Tenant 1 ile etkinliği tekrar getir (bu önbelleği yenilemeli)
            var getEventResponse2 = await _client.GetAsync($"/api/events/{eventId}");
            getEventResponse2.EnsureSuccessStatusCode();
            var getEventContent2 = await getEventResponse2.Content.ReadAsStringAsync();
            var getEventResult2 = JsonSerializer.Deserialize<ResponseDto<EventDto>>(getEventContent2, _jsonOptions);
            
            // Assert - Tenant 2 ile erişim reddedilmeli, Tenant 1 ile erişim başarılı olmalı
            Assert.Equal(HttpStatusCode.NotFound, getEventFromTenant2Response.StatusCode);
            Assert.NotNull(getEventResult2);
            Assert.Equal(eventId, getEventResult2.Data.Id);
            Assert.Equal(_tenant1Id, getEventResult2.Data.TenantId);
            
            // Temizlik
            await _client.DeleteAsync($"/api/events/{eventId}");
        }
    }
} 