# Multi-Tenant Etkinlik Yönetim Sistemi

Multi-Tenant Etkinlik Yönetim Sistemi, farklı organizasyonların (tenant) verilerini tamamen izole bir şekilde tutarak ortak bir sistem üzerinden etkinliklerini yönetmelerine olanak tanıyan bir API ve web uygulamasıdır.

## 📋 Proje Gereksinimleri Checklist

### ✅ Temel Teknolojiler
- [x] ASP.NET Core Web API (.NET 8)
- [x] Entity Framework Core
- [x] SQL Server veritabanı
- [x] Redis ile önbellekleme
- [x] JWT tabanlı kimlik doğrulama
- [x] Swagger/OpenAPI dokümantasyonu

### ✅ Mimari Gereksinimler
- [x] Temiz ve katmanlı mimari (Domain, Application, Infrastructure, API)
- [x] RESTful API en iyi uygulamaları
- [x] Bağımlılık enjeksiyonu (dependency injection)
- [x] Hata yönetimi ve doğrulama mekanizmaları
- [x] Verilere erişim için önbellekleme stratejileri

### ✅ Multi-Tenant Gereksinimler
- [x] Paylaşımlı veritabanı ve paylaşımlı şema yaklaşımı
- [x] Global sorgu filtreleri ile tenant izolasyonu
- [x] JWT token üzerinden tenant kimliği belirleme
- [x] Tenant ayrıştırması için middleware
- [x] Tenant veri sızıntısını önleme güvenlik önlemleri

### ✅ Uygulanan Özellikler
- [x] Kullanıcı yönetimi (kayıt, kimlik doğrulama, roller)
- [x] Tenant yönetimi (oluşturma ve yapılandırma)
- [x] Etkinlik yönetimi (CRUD işlemleri, arama, filtreleme)
- [x] Kayıt yönetimi (katılımcı kaydı, kapasite kontrolü, bekleme listesi)
- [x] Katılımcı yönetimi (bilgi saklama, kayıtlarla ilişkilendirme)
- [x] Temel raporlama (katılım istatistikleri, katılımcı listeleri)

### ✅ Test Katmanı
- [x] Birim testler (domain ve servis katmanları)
- [x] Entegrasyon testleri (API endpoint'leri)
- [x] Tenant izolasyon testleri (veri erişim güvenliği)
- [x] Önbellek geçersiz kılma testleri (cache invalidation)

## 🏗️ Mimari Yapı

Proje, aşağıdaki katmanlardan oluşmaktadır:

- **EventManagement.Domain**: Entity sınıfları, domain servisleri ve arayüzler
- **EventManagement.Application**: İş mantığı, DTO'lar ve servis arayüzleri
- **EventManagement.Infrastructure**: Veritabanı işlemleri, repository'ler, kimlik doğrulama ve harici servisler
- **EventManagement.API**: API endpoint'leri, controller'lar ve middleware'ler
- **EventManagement.Test**: Birim testler, entegrasyon testleri ve özel test senaryoları
- **EventManagement.UI**: (İsteğe bağlı) Web arayüzü

## 🔍 Multi-Tenant Mimari

Sistem, farklı müşterilerin (tenant) verilerini aynı uygulama altyapısı üzerinden yönetebilmelerine olanak tanıyan bir çok kiracılı (multi-tenant) mimari kullanmaktadır.

### Kiracı Tanımlama Stratejileri

1. **Alt Alan Adı (Subdomain)**: Her kiracıya benzersiz bir alt alan adı atanır. (örn. `tenant1.eventmanagement.com`)
2. **HTTP Başlıkları (Headers)**:
   - `X-Tenant`: Subdomain değeri (örn. "tenant1")
   - `X-Tenant-ID`: Kiracı GUID değeri
3. **JWT Token**: Kullanıcı kimlik doğrulaması yapıldığında, token içinde kiracı bilgisi saklanır

### Veritabanı Stratejisi

"Ortak veritabanı, ortak şema" yaklaşımı kullanılmıştır. Tüm kiracılar aynı veritabanını paylaşır, ancak her tabloda kiracı kimliği (TenantId) sütunu bulunur ve tüm veritabanı sorguları otomatik olarak geçerli kiracının kimliğine göre filtrelenir.

### Önbellek (Cache) Stratejisi

Multi-tenant uygulamanın tutarlılığını ve performansını artırmak için:

1. **Tenant-Bazlı Önbellek Anahtarları**: Tüm önbellek anahtarları tenant kimliği ile birleştirilir
2. **Seçici Önbellekleme**: Sık kullanılan, yavaş değişen veriler önbelleğe alınır
3. **Geçersiz Kılma (Invalidation) Mekanizması**: Veri güncellendiğinde, ilgili tenant için önbellek geçersiz hale getirilir
4. **Redis Kullanımı**: Dağıtık önbellek yönetimi için Redis kullanılır

## 🧪 Test Stratejisi

Test katmanımız, uygulamanın tüm yönlerini doğrulamak için dört ana kategoriye ayrılmıştır:

### 1. Servis ve Domain İçin Birim Testler
Domain nesneleri ve servis katmanının iş mantığını doğrulamaya odaklanır:
- Entity'lerin iş kurallarının testi
- Servis metotlarının birim testleri
- Mock nesneler ile bağımlılıkların izolasyonu

### 2. API Endpoint'leri İçin Entegrasyon Testleri
API endpoint'lerinin bütün bir sistem olarak doğru çalıştığını kontrol eder:
- CustomWebApplicationFactory ile in-memory test veritabanı kullanımı
- HTTP isteklerinin ve API yanıtlarının doğruluğu
- Doğrulama (validation) kurallarının testi

### 3. Tenant İzolasyon İşlevselliği İçin Özel Testler
Multi-tenant mimarinin kritik güvenlik özelliklerini doğrular:
- Tenant'lar arası veri sızıntısı olmamasının testi
- Tenant geçişlerinde veri izolasyonunun korunması
- Middleware ve filtre mekanizmalarının doğru çalışması

### 4. Önbellek Geçersiz Kılma Senaryoları İçin Testler
Önbellek mekanizmasının multi-tenant ortamda doğru çalıştığını kontrol eder:
- Tenant'lar arası önbellek izolasyonu
- Veri güncellemelerinin önbelleği doğru şekilde geçersiz kılması
- Eşzamanlı erişimlerde önbellek davranışının tutarlılığı

## 🚀 Kurulum ve Çalıştırma

### Ön Koşullar

- .NET 8 SDK
- SQL Server (Yerel, LocalDB veya Docker)
- Redis (İsteğe bağlı, önbellekleme için)
- Bir IDE (Visual Studio, VS Code vb.)

### Veritabanını Yapılandırma

1. `EventManagement.API/appsettings.json` dosyasındaki veritabanı bağlantı dizesini ayarlayın:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true",
  "Redis": "localhost:6379"
}
```

2. Package Manager Console'da migration'ı uygulayın:

```powershell
cd EventManagement.API
dotnet ef database update
```

### Projeyi Çalıştırma

1. Projeyi klonlayın:

```powershell
git clone https://github.com/softwareEngineerAndDeveloper/EventManagement.git
cd EventManagement
```

2. API'yi başlatın:

```powershell
cd EventManagement.API
dotnet run
```

3. Swagger arayüzüne tarayıcınızdan erişin:

```
https://localhost:2025/api-docs
```

### Testleri Çalıştırma

```powershell
cd EventManagement.Test
dotnet test

# Belirli bir kategori için test çalıştırma
dotnet test --filter "Category=Integration"
dotnet test --filter "FullyQualifiedName~TenantIsolation"
```

## 🔑 Kimlik Doğrulama ve API Kullanımı

### Kullanıcı Oluşturma ve Giriş

```http
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "Test",
  "lastName": "User",
  "phoneNumber": "5551234567"
}
```

```http
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "subdomain": "tenant1"  // Tenant belirtmek için
}
```

Başarılı giriş yanıtı JWT token içerecektir. Bu token'ı diğer API çağrılarında Authorization header olarak kullanın:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Tenant Belirleme

API isteklerinde tenant'ı belirtmek için şu yöntemlerden birini kullanabilirsiniz:

1. Alt alan adı: `https://tenant1.eventmanagement.com/api/events`
2. HTTP header: `X-Tenant-ID: 00000000-0000-0000-0000-000000000000`

### Örnek API Çağrıları

#### Etkinlik Listeleme
```http
GET /api/events
X-Tenant-ID: 00000000-0000-0000-0000-000000000001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Yeni Etkinlik Oluşturma
```http
POST /api/events
X-Tenant-ID: 00000000-0000-0000-0000-000000000001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "title": "Geliştirici Konferansı",
  "description": "Yıllık geliştirici buluşması",
  "startDate": "2023-12-01T09:00:00",
  "endDate": "2023-12-01T17:00:00",
  "location": "İstanbul Kongre Merkezi",
  "maxAttendees": 250,
  "isPublic": true
}
```

#### Etkinliğe Katılımcı Ekleme
```http
POST /api/events/{eventId}/attendees
X-Tenant-ID: 00000000-0000-0000-0000-000000000001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "name": "Ahmet Yılmaz",
  "email": "ahmet@ornek.com",
  "phone": "5551234567"
}
```

## 📚 API Endpoint'leri

### Kimlik Doğrulama ve Kullanıcı Yönetimi
- `POST /api/auth/register`: Yeni kullanıcı kaydı
- `POST /api/auth/login`: Kullanıcı girişi ve JWT token alımı
- `GET /api/users/me`: Mevcut kullanıcı profilini alma
- `PUT /api/users/me`: Kullanıcı profilini güncelleme
- `GET /api/users`: Kullanıcıları listeleme (admin yetkisi gerektirir)

### Tenant Yönetimi
- `POST /api/tenants`: Yeni tenant oluşturma
- `GET /api/tenants/current`: Mevcut tenant bilgilerini alma

### Etkinlik Yönetimi
- `GET /api/events`: Tüm etkinlikleri listeleme (filtreleme desteklenir)
- `GET /api/events/{id}`: Etkinlik detaylarını alma
- `POST /api/events`: Yeni etkinlik oluşturma
- `PUT /api/events/{id}`: Etkinlik güncelleme
- `DELETE /api/events/{id}`: Etkinlik silme

### Katılımcı Yönetimi
- `GET /api/events/{eventId}/attendees`: Etkinlik katılımcılarını listeleme
- `POST /api/events/{eventId}/attendees`: Etkinliğe katılımcı ekleme
- `PUT /api/events/attendees/{id}`: Katılımcı bilgilerini güncelleme
- `DELETE /api/events/attendees/{id}`: Katılımcıyı silme

### Raporlama
- `GET /api/events/{eventId}/statistics`: Etkinlik katılım istatistiklerini alma
- `GET /api/reports/upcoming-events`: Yaklaşan etkinlikler raporunu alma
- `GET /api/reports/tenant-usage`: Tenant kullanım istatistiklerini alma

## 🛠️ Teknik Detaylar

### Kullanılan Teknolojiler
- **.NET 8**: Backend API ve uygulama mantığı
- **Entity Framework Core 8.0**: ORM ve veritabanı işlemleri
- **SQL Server**: Ana veritabanı
- **Redis**: Önbellekleme ve performans optimizasyonu
- **JWT Authentication**: Güvenli API erişimi
- **Swagger/OpenAPI**: API dokümantasyonu
- **xUnit**: Test framework
- **Moq**: Test için mock nesneleri oluşturma
- **FluentAssertions**: Okunabilir test assertion'ları

### Güvenlik Özellikleri
- Tenant izolasyonu ile veri güvenliği
- Rol tabanlı yetkilendirme
- JWT token ile güvenli kimlik doğrulama
- SQL enjeksiyon koruması (Entity Framework parametre temizleme)
- Cross-Origin Resource Sharing (CORS) koruması
- Tenant veri sızıntısı önleme mekanizmaları

### Performans Optimizasyonları
- Redis önbellekleme ile performans artışı
- Veritabanı indeksleme stratejileri
- Lazy-loading ve eager-loading stratejileri
- Async/await pattern ile eşzamanlı işlemler

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. 