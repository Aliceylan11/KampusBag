using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class SearchUserPage : ContentPage
{
    public SearchUserPage()
    {
        InitializeComponent();
        // Klavyeyi hemen aç
        SearchEntry.Focused += (_, _) => { };
        Loaded += (_, _) => SearchEntry.Focus();
    }

    // ── Kullanıcıya Tıklama → ChatDetailPage ─────────────────────────
    private async void OnUserTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not UserSearchModel user) return;

        // Kendi profiline mesaj atmayı engelle
        if (user.Id == ApiService.Session.UserId)
        {
            await DisplayAlert("Uyarı", "Kendinize mesaj gönderemezsiniz.", "Tamam");
            return;
        }

        // Sessiz mod uyarısı
        if (user.MightBeSilent)
        {
            bool proceed = await DisplayAlert(
                "🌙 Sessiz Mod",
                $"{user.FullName} şu an sessiz mod saatlerinde (17:00 sonrası).\n\n" +
                "Mesajınız iletilecek ancak bildirim gönderilmeyecek.\n" +
                "Acil durum için sohbet içindeki 🚨 butonunu kullanın.",
                "Devam Et", "İptal");

            if (!proceed) return;
        }

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: user.FullName,
            courseId: null,
            otherUserId: user.Id,
            isPrivateWithTeacher: user.Role == 2,
            isReadOnly: false
        ));
    }
}
