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
        BindingContext = this;
    }

    // Sadece Hoca ve Admin
    public bool IsTeacherOrAdmin => ApiService.Session.Role == 2 || ApiService.Session.Role == 4;

    // Sadece Temsilci
    public bool IsRepresentative => ApiService.Session.Role == 3;

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
        Routing.RegisterRoute(nameof(CourseDetailPage), typeof(CourseDetailPage)); // YENİ
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

        FlyoutFullNameLabel.Text = string.IsNullOrEmpty(fullName)
            ? "Kullanıcı"
            : fullName;

        FlyoutAvatarLabel.Text = string.IsNullOrEmpty(fullName)
            ? "?"
            : fullName.Trim()[0].ToString().ToUpper();

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

        ApiService.Session.Clear();

        Application.Current.MainPage =
            new NavigationPage(new Views.MainPage());
    }
}
