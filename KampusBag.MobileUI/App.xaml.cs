using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.Views;
using Microsoft.Maui.Dispatching;

namespace KampusBag.MobileUI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // 1. Kritik: Uygulama açılır açılmaz bir sayfa atıyoruz (Çökmeyi önler)
        // Varsayılan olarak giriş sayfasını gösteriyoruz.
        MainPage = new NavigationPage(new Views.MainPage());

        // 2. Arka planda oturumu kontrol et
        _ = CheckLoginStatus();
    }

    private async Task CheckLoginStatus()
    {
        try
        {
            var token = await SecureStorage.GetAsync("token");

            // Token yoksa zaten giriş sayfasındayız, hiçbir şey yapma
            if (string.IsNullOrEmpty(token))
                return;

            // Verileri SecureStorage'dan al
            var userIdStr = await SecureStorage.GetAsync("userId");
            var roleStr = await SecureStorage.GetAsync("role");
            var fullName = await SecureStorage.GetAsync("fullName");

            // Session bilgilerini doldur
            ApiService.Session.Token = token;
            ApiService.Session.FullName = fullName ?? "Kullanıcı";
            ApiService.Session.IsLoggedIn = true;

            if (Guid.TryParse(userIdStr, out Guid userId))
                ApiService.Session.UserId = userId;

            if (int.TryParse(roleStr, out int role))
                ApiService.Session.Role = role;

            // 3. Kritik: Arayüz değişikliğini Ana İşlemci (MainThread) üzerinde yap
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new AppShell();
            });
        }
        catch (Exception ex)
        {
            // Hata olursa uygulamayı kapatma, log yaz ve giriş sayfasında kal
            System.Diagnostics.Debug.WriteLine($"Oturum Hatası: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(new Views.MainPage());
            });
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        // Firebase başlatma hatası uygulamayı patlatmasın diye try-catch
        try
        {
            if (ApiService.Session.IsLoggedIn)
            {
                _ = NotificationService.Instance.InitializeAsync();
            }
        }
        catch { /* Sessizce devam et */ }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();
        try
        {
            if (ApiService.Session.IsLoggedIn)
            {
                _ = NotificationService.Instance.InitializeAsync();
            }
        }
        catch { /* Sessizce devam et */ }
    }
}