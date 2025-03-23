using EventManagement.Application.Services;
using EventManagement.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using StackExchange.Redis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace EventManagement.Test.Services
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IServer> _redisServerMock;
        private readonly Mock<IDatabase> _redisDatabaseMock;
        private readonly ICacheService _cacheService;

        public CacheServiceTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _redisMock = new Mock<IConnectionMultiplexer>();
            _redisServerMock = new Mock<IServer>();
            _redisDatabaseMock = new Mock<IDatabase>();
            
            // Redis mock yapılandırması için spesifik değerler kullanılıyor
            var endpoint = new System.Net.IPEndPoint(0, 0);
            var endpoints = new System.Net.EndPoint[] { endpoint };

            // Mock kurulumları
            // Mock.Get() kullanarak doğrudan çağrıları mocklar üzerinden yapıyoruz
            // Bu yaklaşım, expression tree hatalarını önlemeye yardımcı olur
            var method = typeof(ConnectionMultiplexer).GetMethod("GetServer", new[] { typeof(EndPoint), typeof(object) });
            _redisMock.Setup(m => method.Invoke(m, new object[] { It.IsAny<EndPoint>(), null })).Returns(_redisServerMock.Object);


            // -1 ve null değerleri doğrudan belirtilerek kullanılıyor
            _redisMock.SetupGet(m => m.GetDatabase(-1, null))
                .Returns(_redisDatabaseMock.Object);
                
            _cacheService = new CacheService(_cacheMock.Object, _redisMock.Object);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheHit_ReturnsCachedData()
        {
            // Arrange
            var cacheKey = "test_key";
            var cachedData = new TestData { Id = 1, Name = "Test" };
            var cachedJson = JsonSerializer.Serialize(cachedData);
            
            _cacheMock.Setup(c => c.GetStringAsync(cacheKey, default))
                .ReturnsAsync(cachedJson);
                
            var factoryCalled = false;
            
            // Act
            var result = await _cacheService.GetOrSetAsync<TestData>(
                cacheKey, 
                async () => 
                {
                    factoryCalled = true;
                    return new TestData { Id = 2, Name = "Factory" };
                }, 
                TimeSpan.FromMinutes(10));
                
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
            Assert.False(factoryCalled, "Factory fonksiyonu çağrılmamalıydı");
            
            _cacheMock.Verify(c => c.GetStringAsync(cacheKey, default), Times.Once);
            _cacheMock.Verify(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [Fact]
        public async Task GetOrSetAsync_WhenCacheMiss_CallsFactoryAndCachesResult()
        {
            // Arrange
            var cacheKey = "test_key";
            var factoryData = new TestData { Id = 2, Name = "Factory" };
            
            _cacheMock.Setup(c => c.GetStringAsync(cacheKey, default))
                .ReturnsAsync((string)null);
                
            // Act
            var result = await _cacheService.GetOrSetAsync<TestData>(
                cacheKey, 
                async () => factoryData, 
                TimeSpan.FromMinutes(10));
                
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal("Factory", result.Name);
            
            _cacheMock.Verify(c => c.GetStringAsync(cacheKey, default), Times.Once);
            _cacheMock.Verify(c => c.SetStringAsync(
                cacheKey, 
                It.IsAny<string>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                default), 
                Times.Once);
        }
        
        [Fact]
        public async Task RemoveAsync_RemovesItemFromCache()
        {
            // Arrange
            var cacheKey = "test_key";
            
            // Act
            await _cacheService.RemoveAsync(cacheKey);
            
            // Assert
            _cacheMock.Verify(c => c.RemoveAsync(cacheKey, default), Times.Once);
        }
        
        [Fact]
        public async Task RemoveByPrefixAsync_RemovesItemsWithPrefixFromRedis()
        {
            // Arrange
            var prefix = "test_prefix";
            var pattern = $"{prefix}*";
            var keys = new RedisKey[] { "test_prefix_1", "test_prefix_2" };
            
            // Spesifik değerlerle mocklar oluşturuldu
            _redisServerMock.Setup(s => s.Keys(0, pattern, 1000, CommandFlags.None))
                .Returns(keys);
                
            _redisDatabaseMock.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(k => k.Length == 2), CommandFlags.None))
                .ReturnsAsync(keys.Length);
            
            // Act
            await _cacheService.RemoveByPrefixAsync(prefix);
            
            // Assert
            _redisServerMock.Verify(s => s.Keys(0, pattern, 1000, CommandFlags.None), Times.Once);
            _redisDatabaseMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(k => k.Length == 2), CommandFlags.None), Times.Once);
        }
        
        // Tenant İzolasyonu Testi
        [Fact]
        public async Task GetOrSetAsync_WithTenantPrefix_IsolatesTenantData()
        {
            // Arrange
            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();
            
            var tenant1CacheKey = $"tenant:{tenant1Id}:data";
            var tenant2CacheKey = $"tenant:{tenant2Id}:data";
            
            var tenant1Data = new TestData { Id = 1, Name = "Tenant 1 Data" };
            var tenant2Data = new TestData { Id = 2, Name = "Tenant 2 Data" };
            
            _cacheMock.Setup(c => c.GetStringAsync(tenant1CacheKey, default))
                .ReturnsAsync((string)null);
            _cacheMock.Setup(c => c.GetStringAsync(tenant2CacheKey, default))
                .ReturnsAsync((string)null);
            
            // Act - Tenant 1 verisini önbelleğe al
            var result1 = await _cacheService.GetOrSetAsync<TestData>(
                tenant1CacheKey, 
                async () => tenant1Data, 
                TimeSpan.FromMinutes(10));
                
            // Act - Tenant 2 verisini önbelleğe al
            var result2 = await _cacheService.GetOrSetAsync<TestData>(
                tenant2CacheKey, 
                async () => tenant2Data, 
                TimeSpan.FromMinutes(10));
                
            // Assert
            Assert.NotEqual(result1.Id, result2.Id);
            Assert.NotEqual(result1.Name, result2.Name);
            
            _cacheMock.Verify(c => c.SetStringAsync(
                tenant1CacheKey, 
                It.IsAny<string>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                default), 
                Times.Once);
                
            _cacheMock.Verify(c => c.SetStringAsync(
                tenant2CacheKey, 
                It.IsAny<string>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                default), 
                Times.Once);
        }
        
        public class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
} 