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

## 🏗️ Mimari Yapı

Proje, aşağıdaki katmanlardan oluşmaktadır:

- **EventManagement.Domain**: Entity sınıfları, domain servisleri ve arayüzler
- **EventManagement.Application**: İş mantığı, DTO'lar ve servis arayüzleri
- **EventManagement.Infrastructure**: Veritabanı işlemleri, repository'ler, kimlik doğrulama ve harici servisler
- **EventManagement.API**: API endpoint'leri, controller'lar ve middleware'ler
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
git clone https://github.com/yourusername/EventManagement.git
cd EventManagement
```

2. API'yi başlatın:

```powershell
cd EventManagement.API
dotnet run
```

3. Swagger arayüzüne tarayıcınızdan erişin:

```
https://localhost:5001/api-docs
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
  "password": "SecurePassword123!"
}
```

Başarılı giriş yanıtı JWT token içerecektir. Bu token'ı diğer API çağrılarında Authorization header olarak kullanın:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Tenant Belirleme

API isteklerinde tenant'ı belirtmek için şu yöntemlerden birini kullanabilirsiniz:

1. Alt alan adı: `https://tenant1.eventmanagement.com/api/events`
2. HTTP header: `X-Tenant: tenant1` veya `X-Tenant-ID: 00000000-0000-0000-0000-000000000000`

### Örnek API Çağrıları

#### Etkinlik Listeleme
```http
GET /api/events
```

#### Yeni Etkinlik Oluşturma
```http
POST /api/events
{
  "title": "Geliştirici Konferansı",
  "description": "Yıllık geliştirici buluşması",
  "startDate": "2023-12-01T09:00:00",
  "endDate": "2023-12-01T17:00:00",
  "location": "İstanbul Kongre Merkezi",
  "capacity": 250,
  "isPublic": true
}
```

#### Etkinliğe Katılımcı Kaydetme
```http
POST /api/events/{eventId}/registrations
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
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

### Kayıt Yönetimi
- `GET /api/events/{eventId}/registrations`: Etkinlik kayıtlarını listeleme
- `POST /api/events/{eventId}/registrations`: Etkinliğe katılımcı kaydetme
- `PUT /api/events/{eventId}/registrations/{id}`: Kayıt durumunu güncelleme
- `DELETE /api/events/{eventId}/registrations/{id}`: Kaydı iptal etme

### Raporlama
- `GET /api/events/{eventId}/statistics`: Etkinlik katılım istatistiklerini alma
- `GET /api/reports/upcoming-events`: Yaklaşan etkinlikler raporunu alma

## 🛠️ Teknik Detaylar

### Kullanılan Teknolojiler
- **.NET 8**: Backend API ve uygulama mantığı
- **Entity Framework Core 8.0**: ORM ve veritabanı işlemleri
- **SQL Server**: Ana veritabanı
- **Redis**: Önbellekleme ve performans optimizasyonu
- **JWT Authentication**: Güvenli API erişimi
- **Swagger/OpenAPI**: API dokümantasyonu

### Güvenlik Özellikleri
- Tenant izolasyonu ile veri güvenliği
- Rol tabanlı yetkilendirme
- JWT token ile güvenli kimlik doğrulama
- SQL enjeksiyon koruması (Entity Framework parametre temizleme)
- Cross-Origin Resource Sharing (CORS) koruması

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. 