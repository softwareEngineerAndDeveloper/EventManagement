using EventManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Test.Integration
{
    public class UserApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public UserApiIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetAllUsers_ReturnsAllUsersForTenant()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");

            // Act
            var response = await _client.GetAsync("/api/users");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<List<UserDto>>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Count > 0);

            // Tüm kullanıcıların aynı tenant'a ait olup olmadığını kontrol et
            foreach (var user in result.Data)
            {
                Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), user.TenantId);
            }
        }

        [Fact]
        public async Task GetCurrentUser_ReturnsAuthenticatedUser()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");

            // Act
            var response = await _client.GetAsync("/api/users/me");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<UserDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("admin@test.com", result.Data.Email);
            Assert.Equal("Admin", result.Data.FirstName);
            Assert.Equal("User", result.Data.LastName);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), result.Data.TenantId);
        }

        [Fact]
        public async Task GetUserById_ReturnsCorrectUser()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011"); // Test verisindeki admin kullanıcı ID'si

            // Act
            var response = await _client.GetAsync($"/api/users/{adminUserId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<UserDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(adminUserId, result.Data.Id);
            Assert.Equal("admin@test.com", result.Data.Email);
            Assert.Equal("Admin", result.Data.FirstName);
            Assert.Equal("User", result.Data.LastName);
        }

        [Fact]
        public async Task UpdateUser_UpdatesUserInformation()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011"); // Test verisindeki admin kullanıcı ID'si
            
            var updateDto = new UpdateUserDto
            {
                FirstName = "Güncellenmiş Ad",
                LastName = "Güncellenmiş Soyad",
                PhoneNumber = "5551112233"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/users/{adminUserId}", updateDto);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<UserDto>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(adminUserId, result.Data.Id);
            Assert.Equal("Güncellenmiş Ad", result.Data.FirstName);
            Assert.Equal("Güncellenmiş Soyad", result.Data.LastName);
            Assert.Equal("5551112233", result.Data.PhoneNumber);
        }

        [Fact]
        public async Task GetUserRoles_ReturnsUserRoles()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000001");
            
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011"); // Test verisindeki admin kullanıcı ID'si
            var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000010"); // Test verisindeki admin rol ID'si

            // Act
            var response = await _client.GetAsync($"/api/users/{adminUserId}/roles");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ResponseDto<List<Guid>>>(content, _jsonOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Contains(adminRoleId, result.Data);
        }

        [Fact]
        public async Task GetUserWithWrongTenant_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Tenant2 değeri ile istek yap
            _client.DefaultRequestHeaders.Add("X-Tenant-ID", "00000000-0000-0000-0000-000000000002");
            
            // Tenant1'e ait kullanıcıyı Tenant2 ile getirmeye çalış
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000011"); // Tenant1'e ait admin kullanıcı ID'si

            // Act
            var response = await _client.GetAsync($"/api/users/{adminUserId}");
            
            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
} 