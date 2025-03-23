# EventManagement Test Projesi

Bu proje, Multi-Tenant Etkinlik Yönetimi API'si için kapsamlı test senaryoları içerir. Test yapısı, farklı sorumluluk alanlarını kapsayan ve uygulamanın çeşitli yönlerini doğrulayan dört ana kategoriye ayrılmıştır.

## Test Kategorileri

### 1. Servis ve Domain İçin Birim Testler
- `Domain/`: Domain nesnelerinin ve iş kurallarının doğruluğunu test eder
  - EventTests, AttendeeTests vb.
- `Services/`: Servis katmanının business logic fonksiyonlarını test eder
  - EventServiceTests, UserServiceTests, RegistrationServiceTests vb.
  - TenantServiceTests, CacheServiceTests vb.

### 2. API Endpoint'leri İçin Entegrasyon Testleri
- `Integration/`: API endpoint'lerinin entegrasyon testleri
  - EventApiIntegrationTests, AttendeeApiIntegrationTests, UserApiIntegrationTests vb.
  - CustomWebApplicationFactory: Entegrasyon testleri için test altyapısı

### 3. Tenant İzolasyon İşlevselliği İçin Özel Testler
- `Integration/TenantIsolationTests.cs`: Tenant'lar arası veri izolasyonunun doğruluğunu test eder
- `Integration/TenantSwitchingTests.cs`: Tenant'lar arası geçişlerin doğru çalıştığını test eder
- `Middleware/TenantMiddlewareTests.cs`: Tenant middleware'inin doğru çalıştığını test eder

### 4. Önbellek Geçersiz Kılma Senaryoları İçin Testler
- `Integration/TenantCacheInvalidationTests.cs`: Önbelleğin multi-tenant ortamda doğru çalıştığını test eder
  - Tenant'lar arası önbellek izolasyonu
  - Veri güncellemeleri sonrası önbellek geçersiz kılma
  - Eşzamanlı erişimler sırasında önbellek davranışı

## Kullanılan Teknolojiler

- **xUnit**: Test framework
- **Moq**: Mock nesneleri oluşturmak için
- **FluentAssertions**: Daha okunabilir assertion'lar için
- **EntityFrameworkCore.InMemory**: Veritabanı işlemleri için in-memory veritabanı

## Testleri Çalıştırma

```bash
# Tüm testleri çalıştırma
dotnet test

# Belirli bir kategoriyi çalıştırma
dotnet test --filter "Category=Integration"
dotnet test --filter "FullyQualifiedName~TenantIsolation"

# Test coverage raporu
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Visual Studio Test Explorer üzerinden çalıştırma
# Test > Run All Tests menüsünü kullanabilirsiniz
```

## Test Prensipleri

- **Bağımsız Testler**: Her test bağımsız olmalı ve diğer testlere bağımlı olmamalıdır
- **Arrange-Act-Assert (AAA)**: Tüm testler bu desene göre yapılandırılmıştır
- **İzolasyon**: Mock nesneler ile servis bağımlılıkları izole edilmiştir
- **Deterministik**: Testler her zaman aynı sonucu vermelidir
- **Okunabilirlik**: Test isimlendirmeleri ve yapıları test edilen davranışı açıkça göstermelidir

## Multi-Tenant Test Stratejisi

Multi-tenant uygulamalarda şu test prensipleri önemlidir:

1. **Veri İzolasyonu**: Tenant'lar arası veri sızıntısı olmamalıdır
2. **Yetkilendirme**: Bir tenant'ın kullanıcısı sadece kendi tenant'ının verilerine erişebilmelidir
3. **Paylaşılan Kaynaklar**: Önbellek gibi paylaşılan kaynaklar tenant bazında izole edilmelidir
4. **Paralel İşlemler**: Farklı tenant'ların eşzamanlı işlemleri birbirini etkilememelidir

