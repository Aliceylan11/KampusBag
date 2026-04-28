# KampusBag 🎒

KampusBag, üniversite öğrencileri ve akademisyenler için özel olarak tasarlanmış, güvenli ve etkileşimli bir kampüs içi iletişim platformudur. Mobil arayüzü .NET MAUI ile, arka plan servisleri ise ASP.NET Core Web API kullanılarak modern ve ölçeklenebilir bir mimariyle geliştirilmektedir.

## 🚀 Projenin Amacı
Üniversite içindeki ders grupları, duyurular ve birebir iletişim süreçlerini tek bir çatı altında toplamak; öğrencilerin ve akademisyenlerin güvenli bir şekilde (kurumsal e-posta doğrulaması ile) iletişim kurmasını sağlamaktır.

## 🏗️ Kullanılan Teknolojiler
Bu proje, "N-Tier Architecture" (Çok Katmanlı Mimari) prensiplerine uygun olarak geliştirilmektedir:

* **Mobil Uygulama (Client):** .NET MAUI (iOS, Android, Windows, MacCatalyst destekli)
* **Backend (API):** ASP.NET Core 8 Web API
* **Veritabanı:** PostgreSQL
* **ORM:** Entity Framework Core (Code-First Yaklaşımı)
* **Altyapı & Konteynerizasyon:** Docker & Docker Compose
* **Kimlik Doğrulama:** JWT (JSON Web Token) & E-posta OTP Doğrulama *(Geliştirme aşamasında)*

## 📂 Proje Mimarisi (Katmanlar)
1. **KampusBag.Core:** Projenin kalbi. Entity'ler (User, Course, Message vb.), DTO'lar ve Interface'ler bu katmanda yer alır.
2. **KampusBag.Infrastructure:** Veritabanı bağlantıları (DbContext), Repository implementasyonları, Migrations ve şifreleme (Hash) işlemleri burada yönetilir.
3. **KampusBag.Application:** İş kuralları ve servislerin (UserService, MessageService vb.) bulunduğu katmandır.
4. **KampusBag.WebAPI:** Dış dünyaya açılan kapımız. Mobil uygulamanın istek attığı Controller'ları (UsersController vb.) barındırır. Swagger entegrasyonu ile test edilebilir durumdadır.
5. **KampusBag.MobileUI:** Kullanıcıyla etkileşime giren, .NET MAUI kullanılarak tasarlanan modern mobil arayüzdür.

## ✅ Mevcut Durum ve Tamamlanan İşlemler
- [x] N-Katmanlı mimari kurulumu tamamlandı.
- [x] Docker üzerinden PostgreSQL veritabanı ayağa kaldırıldı.
- [x] Entity Framework Core ile veritabanı tabloları (Code-First) oluşturuldu.
- [x] API üzerinden güvenli "Kayıt Ol" (Register) işlemi ve şifre hashleme (SHA256) entegre edildi.
- [x] Güvenlik için 6 haneli kod ile "E-Posta Doğrulama" (OTP) algoritması ve API ucu yazıldı.
- [x] MAUI tarafında API ile haberleşen `ApiService` yazıldı ve Login/Register arayüzleri API'ye bağlandı.

## ⚙️ Kurulum ve Çalıştırma
Projeyi lokalinizde çalıştırmak için:
1. Repoyu klonlayın: `git clone https://github.com/KULLANICI_ADIN/KampusBag.git`
2. Ana dizinde terminali açıp veritabanını ayağa kaldırın: `docker-compose up -d`
3. Web API projesini çalıştırın ve Swagger üzerinden endpointleri inceleyin.
4. Mobil projeyi (MobileUI) Windows Machine veya Android Emulator üzerinde başlatın.
