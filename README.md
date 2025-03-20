# Etkinlik Yönetim Sistemi

Etkinlik Yönetim Sistemi, organizasyonların ve şirketlerin etkinliklerini yönetmelerine olanak sağlayan çok kiracılı (multi-tenant) bir web uygulamasıdır.

## Özellikler

- **Çok Kiracılı Mimari**: Her müşteri için ayrı alt alan adları (subdomain) ve izole veri
- **Etkinlik Yönetimi**: Etkinlik oluşturma, düzenleme, listeleme ve silme
- **Katılımcı Kaydı**: Etkinliklere katılımcı kayıt yönetimi
- **Kullanıcı Yönetimi**: Rol tabanlı kimlik doğrulama ve yetkilendirme
- **Raporlama**: Etkinlik katılımı, doluluk oranı ve daha fazlası için raporlar

## Teknoloji Yığını

- **.NET 8**: Backend API ve uygulama mantığı için
- **Entity Framework Core**: Veritabanı işlemleri için ORM
- **SQL Server**: Veritabanı
- **JWT Kimlik Doğrulama**: Güvenli API erişimi için

## Kurulum ve Çalıştırma

### Ön Koşullar

- .NET 8 SDK
- SQL Server (Yerel veya Docker)
- Bir IDE (Visual Studio, VS Code vb.)

### Veritabanını Yapılandırma

1. `EventManagement.API/appsettings.json` dosyasını açın ve veritabanı bağlantı dizesini ayarlayın:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

2. Package Manager Console'da Entity Framework migrations'ı uygulayın:

```powershell
cd EventManagement.API
dotnet ef database update
```

### Projeyi Çalıştırma

1. Projeyi klonlayın:

```powershell
git clone https://github.com/your-username/event-management.git
cd event-management
```

2. API'yi başlatın:

```powershell
cd EventManagement.API
dotnet run
```

3. Tarayıcınızda Swagger arayüzüne erişin:

```
https://localhost:5001/swagger
```

## API Kullanımı

API'yi kullanabilmek için önce bir token almanız gerekir:

```http
POST /api/auth/login
{
  "email": "admin@example.com",
  "password": "YourPassword123!"
}
```

Başarılı giriş yanıtı JWT token içerecektir. Bu token'ı diğer API çağrılarında Authorization header'ında kullanın:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Çoklu Kiracı Kullanımı

Her kiracı kendi alt alan adına sahiptir. API isteklerinde kiracı kimliğini belirtmek için aşağıdaki yöntemlerden birini kullanabilirsiniz:

1. Alt alan adı: `https://tenant1.eventmanagement.com/api/events`
2. HTTP header: `X-Tenant: tenant1` veya `X-Tenant-ID: 00000000-0000-0000-0000-000000000000`

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakın. 

## Mimari Açıklaması

# Çok Kiracılı (Multi-Tenant) Mimari Açıklaması

Bu belge, Etkinlik Yönetim Sistemi'nin çok kiracılı mimarisini açıklamaktadır.

## Genel Bakış

Etkinlik Yönetim Sistemi, farklı organizasyonların (kiracıların) aynı uygulama altyapısı üzerinden kendi etkinliklerini bağımsız olarak yönetebilmelerine olanak tanıyan bir çok kiracılı (multi-tenant) mimari kullanmaktadır. Her kiracı, kendi izole verisine sahiptir ve diğer kiracıların verilerine erişemez.

## Kiracı Tanımlama Stratejisi

Sistemimiz kiracıları aşağıdaki yöntemlerle tanımlar:

1. **Alt Alan Adı (Subdomain) Tanıma**: Her kiracıya benzersiz bir alt alan adı atanır. Örneğin, tenant1.eventmanagement.com

2. **HTTP Başlıkları (Headers)**: İstemciler, API isteklerinde kiracı kimliğini belirtmek için HTTP başlıklarını kullanabilir:
   - `X-Tenant`: Alt alan adı değerini içerir (örn. "tenant1")
   - `X-Tenant-ID`: Kiracı GUID değerini içerir

3. **JWT Token İçinde Kiracı Bilgisi**: Kullanıcı kimlik doğrulaması yapıldığında, JWT token içinde kullanıcının hangi kiracıya ait olduğu da saklanır. Böylece, API isteklerinde kullanıcının sadece kendi kiracısının verilerine erişebilmesi sağlanır.

## Mimari Bileşenler

### TenantMiddleware

Sistemin merkezinde `TenantMiddleware` yer alır. Bu middleware, her API isteğini yakalayarak:

1. İstek URL'sindeki alt alan adını kontrol eder
2. HTTP başlıklarını kontrol eder
3. JWT token'daki kiracı bilgisini doğrular
4. İlgili kiracıyı veritabanından bulur ve HttpContext.Items'a ekler

Böylece, uygulamanın diğer bileşenleri HttpContext aracılığıyla geçerli kiracıya kolayca erişebilir.

### Veritabanı Stratejisi

Uygulama, "ortak veritabanı, ayrı şema" yaklaşımını kullanır. Tüm kiracılar aynı veritabanını paylaşır, ancak her tabloda kiracı kimliği (TenantId) sütunu bulunur. Tüm veritabanı sorguları, otomatik olarak geçerli kiracının kimliğine göre filtrelenir.

### Repository Katmanı

Repository sınıfları, veritabanı sorgularını gerçekleştirirken TenantId filtresini otomatik olarak ekler. Bu sayede, tüm veriler kiracı düzeyinde izole edilir ve bir kiracı diğer kiracının verilerine erişemez.

### Servis Katmanı

Servis katmanı, business logic'i uygularken her zaman kiracı kontekstini göz önünde bulundurur. Tüm servis metodları, TenantId parametresini alır ve bu parametre ile repository katmanına istek yapar.

## Güvenlik Önlemleri

- Bir kullanıcı, JWT token'ında belirtilen kiracıdan farklı bir kiracıya ait verilere erişmeye çalıştığında hata alır
- Kiracı bulunamazsa 404 hatası döndürülür
- Kiracı uyuşmazlığı durumunda 403 (Forbidden) hatası döndürülür

## Örnek Akış

1. Kullanıcı tenant1.eventmanagement.com üzerinden giriş yapar
2. Sistem, alt alan adından kiracıyı tanımlar
3. Kullanıcı kimlik doğrulaması başarılı olursa, JWT token içinde 'tenant_id' talebi (claim) yer alır
4. Kullanıcı etkinlikleri listelemek istediğinde, API otomatik olarak sadece 'tenant1' kiracısına ait etkinlikleri getirir

Bu mimari, farklı organizasyonların aynı uygulama altyapısını kullanarak kendi izole ortamlarına sahip olmalarını sağlar. 