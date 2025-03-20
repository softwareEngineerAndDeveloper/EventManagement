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