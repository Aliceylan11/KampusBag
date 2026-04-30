using KampusBag.MobileUI.Views.Auth;
using KampusBag.MobileUI.Views.Chats;

namespace KampusBag.MobileUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Mevcut rotalar
        Routing.RegisterRoute(nameof(ChatDetailPage), typeof(ChatDetailPage));
        Routing.RegisterRoute(nameof(CreateCoursePage), typeof(CreateCoursePage));
        Routing.RegisterRoute(nameof(JoinCoursePage), typeof(JoinCoursePage));
        Routing.RegisterRoute(nameof(SearchUserPage), typeof(SearchUserPage));
        Routing.RegisterRoute(nameof(EmailVerificationPage), typeof(EmailVerificationPage));

        // YENİ ROTALAR - Şifre sıfırlama
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
        Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
    }
}
