using KampusBag.MobileUI.Views.Auth;
using KampusBag.MobileUI.Views.Chats;

namespace KampusBag.MobileUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Navigasyon rotalarını kaydediyoruz (GoToAsync ile gitmek için)
        Routing.RegisterRoute(nameof(ChatDetailPage), typeof(ChatDetailPage));
        Routing.RegisterRoute(nameof(CreateCoursePage), typeof(CreateCoursePage));
        Routing.RegisterRoute(nameof(JoinCoursePage), typeof(JoinCoursePage));
        Routing.RegisterRoute(nameof(SearchUserPage), typeof(SearchUserPage));
        Routing.RegisterRoute(nameof(EmailVerificationPage), typeof(EmailVerificationPage));
    }
}