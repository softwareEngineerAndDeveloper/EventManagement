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