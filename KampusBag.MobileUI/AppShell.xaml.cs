using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.Views.Auth;
using KampusBag.MobileUI.Views.Chats;
using KampusBag.MobileUI.Views.Profile;

namespace KampusBag.MobileUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
        LoadFlyoutHeader();
    }

    // ════════════════════════════════════════
    // ROUTE KAYITLARI
    // ════════════════════════════════════════
    private void RegisterRoutes()
    {
        // Auth akışı
        Routing.RegisterRoute(nameof(EmailVerificationPage), typeof(EmailVerificationPage));
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
        Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));

        // Chat akışı
        Routing.RegisterRoute(nameof(ChatDetailPage), typeof(ChatDetailPage));
        Routing.RegisterRoute(nameof(JoinCoursePage), typeof(JoinCoursePage));
        Routing.RegisterRoute(nameof(CreateCoursePage), typeof(CreateCoursePage));
        Routing.RegisterRoute(nameof(SearchUserPage), typeof(SearchUserPage));
    }

    // ════════════════════════════════════════
    // FLYOUT HEADER — Session'dan Veri Yükleme
    // ════════════════════════════════════════
    private void LoadFlyoutHeader()
    {
        var fullName = ApiService.Session.FullName;
        var role = ApiService.Session.Role;

        // Ad Soyad
        FlyoutFullNameLabel.Text = string.IsNullOrEmpty(fullName)
            ? "Kullanıcı"
            : fullName;

        // Avatar baş harf
        FlyoutAvatarLabel.Text = string.IsNullOrEmpty(fullName)
            ? "?"
            : fullName.Trim()[0].ToString().ToUpper();

        // Rol etiketi
        FlyoutRoleLabel.Text = role switch
        {
            1 => "👨‍🎓  Öğrenci",
            2 => "👨‍🏫  Akademisyen",
            3 => "🏅  Sınıf Temsilcisi",
            _ => "Kullanıcı"
        };
    }

    // ════════════════════════════════════════
    // FLYOUT FOOTER — Güvenli Çıkış
    // ════════════════════════════════════════
    private async void OnFlyoutLogoutClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Çıkış Yap",
            "Oturumunuzu kapatmak istediğinize emin misiniz?",
            "Evet, Çık",
            "İptal");

        if (!confirmed) return;

        // Session temizle
        ApiService.Session.Clear();

        // Navigasyon stack'ini sıfırla ve başlangıca dön
        Application.Current.MainPage =
            new NavigationPage(new Views.MainPage());
    }
}
