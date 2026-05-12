# 🎓 KampusBag - Üniversite Etkileşim ve İletişim Platformu

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![MAUI](https://img.shields.io/badge/MAUI-Cross%20Platform-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql)
![SignalR](https://img.shields.io/badge/SignalR-Real%20Time-0078D4?style=for-the-badge)
![Security](https://img.shields.io/badge/Security-JWT%20%7C%20AES256-brightgreen?style=for-the-badge)

KampusBag, üniversite öğrencileri ve akademisyenler arasındaki iletişimi dijitalleştiren, güvenli ve gerçek zamanlı bir mobil platformdur. Proje, kurumsal **Onion (Soğan) Mimarisi** prensiplerine uygun olarak geliştirilmiştir.

## 🚀 Öne Çıkan Özellikler

- **Gerçek Zamanlı Mesajlaşma:** SignalR altyapısı ile ders gruplarında ve özel sohbetlerde anlık iletişim.
- **Uçtan Uca Şifreleme (E2EE):** Tüm mesaj içerikleri veritabanında **AES-256** standardı ile şifrelenmiş olarak tutulur. Mobil arayüze ulaşana kadar şifreli kalır.
- **Ders & Topluluk Yönetimi:** Hocaların ders grubu oluşturması ve öğrencilerin benzersiz kodlarla bu gruplara katılması.
- **Acil Durum Mesaj Hakkı:** Öğrenciler için dönemlik tanımlı (örn: 3 adet) öncelikli mesajlaşma kotası.
- **Güvenli Kimlik Doğrulama:** JWT (JSON Web Token) tabanlı oturum yönetimi ve e-posta OTP (Doğrulama Kodu) ile güvenli kayıt.

## 🏗 Mimari ve Teknolojiler

Proje, bağımlılıkların sıkı kontrol edildiği **N-Katmanlı (N-Tier) Onion Architecture** kullanılarak tasarlanmıştır.

- **Client (Mobil):** .NET MAUI (MVVM Pattern)
- **Backend (API):** ASP.NET Core 8 Web API
- **Veritabanı:** PostgreSQL (Entity Framework Core Code-First)
- **Konteynerizasyon:** Docker & Docker Compose
- **İletişim & Güvenlik:** SignalR, JWT Bearer, AES Encryption, Ngrok (Test Tünelleme)

### Proje Katmanları
1. `KampusBag.Core`: Entity, DTO, Enum, Interface ve Configuration nesneleri. Bağımsız katman.
2. `KampusBag.Infrastructure`: Veritabanı işlemleri (Repository), SignalR konfigürasyonları, JWT üretim ve AES şifreleme servisleri.
3. `KampusBag.Application`: İş mantığı (Business Logic) kurallarının işletildiği katman.
4. `KampusBag.WebAPI`: Dış dünyaya açılan kapı, Endpointler ve Hub'lar.
5. `KampusBag.MobileUI`: Son kullanıcı arayüzü ve cihaz servisleri.

## 🛠 Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Visual Studio 2022 (MAUI iş yükü yüklü)

### 1. Veritabanını Ayağa Kaldırma (Docker)
Proje kök dizininde terminali açın ve aşağıdaki komutu çalıştırarak PostgreSQL veritabanını başlatın:
```bash
docker-compose up -d
