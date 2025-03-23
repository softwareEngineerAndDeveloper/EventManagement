using EventManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    public class AttendeeApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AttendeeApiIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetAttendeesByEventId_ReturnsAttendees()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            // Test etkinliği için katılımcı oluştur
            var eventId = Guid.Parse("00000000-0000-0000-0000-000000000101"); // Tenant 1'e ait test etkinliği
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Test Katılımcı",
                Email = "test-attendee@example.com",
                Phone = "5551112233",
                Notes = "Entegrasyon testi için oluşturuldu"
            };
            
            // Önce yeni katılımcı oluştur
            var createResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto);
            createResponse.EnsureSuccessStatusCode();

            // Act
            var response = await _client.GetAsync($"/api/events/{eventId}/attendees");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<List<AttendeeDto>>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Count > 0);
            Assert.Contains(result.Data, a => a.Email == "test-attendee@example.com");
        }

        [Fact]
        public async Task GetAttendeesByEventId_WithWrongTenant_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            // Tenant 2'nin etkinliğine Tenant 1 ile erişmeye çalış
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            var eventId = Guid.Parse("00000000-0000-0000-0000-000000000201"); // Tenant 2'ye ait test etkinliği

            // Act
            var response = await _client.GetAsync($"/api/events/{eventId}/attendees");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateAttendee_WithValidData_ReturnsCreatedAttendee()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            var eventId = Guid.Parse("00000000-0000-0000-0000-000000000101"); // Tenant 1'e ait test etkinliği
            var uniqueEmail = $"test-attendee-{Guid.NewGuid()}@example.com"; // Benzersiz e-posta adresi
            
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "Yeni Test Katılımcı",
                Email = uniqueEmail,
                Phone = "5559998877",
                Notes = "Entegrasyon testi için oluşturuldu"
            };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Yeni Test Katılımcı", result.Data.Name);
            Assert.Equal(uniqueEmail, result.Data.Email);
            Assert.Equal("5559998877", result.Data.Phone);
            Assert.Equal(eventId, result.Data.EventId);
        }

        [Fact]
        public async Task CreateAttendee_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            var eventId = Guid.Parse("00000000-0000-0000-0000-000000000101"); // Tenant 1'e ait test etkinliği
            var email = "duplicate-test@example.com";
            
            // İlk katılımcıyı oluştur
            var createAttendeeDto1 = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "İlk Katılımcı",
                Email = email,
                Phone = "5551112233"
            };
            
            var response1 = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto1);
            response1.EnsureSuccessStatusCode();
            
            // Aynı e-posta ile ikinci katılımcıyı oluşturmaya çalış
            var createAttendeeDto2 = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "İkinci Katılımcı",
                Email = email,
                Phone = "5554443322"
            };

            // Act
            var response2 = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto2);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
            var content = await response2.Content.ReadAsStringAsync();
            Assert.Contains("zaten mevcut", content); // Hata mesajında "zaten mevcut" ifadesi olmalı
        }

        [Fact]
        public async Task GetAttendeeById_ReturnsCorrectAttendee()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            // Önce bir katılımcı oluştur
            var eventId = Guid.Parse("00000000-0000-0000-0000-000000000101"); // Tenant 1'e ait test etkinliği
            var createAttendeeDto = new CreateAttendeeDto
            {
                EventId = eventId,
                Name = "ID Testi Katılımcı",
                Email = $"id-test-{Guid.NewGuid()}@example.com",
                Phone = "5557778899"
            };
            
            var createResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/attendees", createAttendeeDto);
            createResponse.EnsureSuccessStatusCode();
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createResult = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(createContent, _jsonOptions);
            var attendeeId = createResult?.Data?.Id;

            // Act
            var response = await _client.GetAsync($"/api/events/attendees/{attendeeId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<AttendeeDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(attendeeId, result.Data.Id);
            Assert.Equal("ID Testi Katılımcı", result.Data.Name);
            Assert.Equal(eventId, result.Data.EventId);
        }
    }
} 