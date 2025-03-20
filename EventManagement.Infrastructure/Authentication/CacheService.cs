using EventManagement.Application.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace EventManagement.Infrastructure.Authentication
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true 
        };

        public CacheService(IDistributedCache cache, IConnectionMultiplexer redisConnection)
        {
            _cache = cache;
            _redisConnection = redisConnection;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            var cachedData = await _cache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var deserializedData = JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
                    if (deserializedData != null)
                    {
                        return deserializedData;
                    }
                }
                catch
                {
                    // Deserializetion hatası durumunda, önbelleği silip yeni değeri al
                    await _cache.RemoveAsync(key);
                }
            }
            
            // Önbellekte yoksa veya deserializetion hatası olduysa, değeri üret
            var result = await factory();
            
            // Sonucu önbelleğe kaydet
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            
            var serializedData = JsonSerializer.Serialize(result, _jsonOptions);
            await _cache.SetStringAsync(key, serializedData, options);
            
            return result;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            // Redis'in SCAN ve DEL komutlarını kullanarak belirli bir önek ile eşleşen tüm anahtarları siler
            if (_redisConnection != null)
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                var db = _redisConnection.GetDatabase();
                
                // Redis SCAN komutuyla belirtilen öne ile eşleşen tüm anahtarları bul
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                
                if (keys.Any())
                {
                    // Bulunan tüm anahtarları sil
                    await db.KeyDeleteAsync(keys);
                }
            }
            else
            {
                throw new InvalidOperationException("Redis bağlantısı kurulmamış. Bu metot, Redis'i gerektiren prefix tabanlı anahtar silme işlemi için gereklidir.");
            }
        }
    }
} 