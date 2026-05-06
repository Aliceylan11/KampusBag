using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatListPage : ContentPage
{
    private ChatListViewModel _vm => (ChatListViewModel)BindingContext;

    public ChatListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadChatsAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        => _vm.SearchCommand.Execute(e.NewTextValue);
    private async void OnSearchUserClicked(object sender, EventArgs e)
    => await Navigation.PushAsync(new SearchUserPage());
    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());

    // ── Resmi Kanal Tıklama ───────────────────────────────────────────
    private async void OnOfficialChannelTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        // Öğrenci → sadece okuyabilir
        bool canWrite = ApiService.Session.Role is 2 or 3;

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: item.DisplayName,
            courseId: item.CourseId,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: item.IsLocked && !canWrite
        ));
    }

    // ── Çalışma Odası Tıklama ─────────────────────────────────────────
    private async void OnStudyRoomTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: item.DisplayName,
            courseId: item.CourseId,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: false
        ));
    }

    // ── Özel Mesaj Tıklama (Sessiz Mod Uyarısı) ───────────────────────
    private async void OnPrivateChatTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        // Sessiz Mod Uyarısı
        if (item.IsSilentMode)
        {
            bool proceed = await DisplayAlert(
                "🌙 Sessiz Mod Aktif",
                $"{item.DisplayName} şu an sessiz mod saatlerinde (17:00 sonrası).\n\n" +
                "Normal mesajınız iletilecek ancak bildirim gönderilmeyecek. " +
                "Acil durumlar için 🚨 butonunu kullanın.",
                "Devam Et",
                "İptal");

            if (!proceed) return;
        }

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: item.DisplayName,
            courseId: null,
            otherUserId: item.OtherUserId,
            isPrivateWithTeacher: item.OtherUserRole == 2,
            isReadOnly: false
        ));
    }

    // ── Yeni Sohbet ───────────────────────────────────────────────────
    private async void OnNewChatClicked(object sender, EventArgs e)
    {
        var options = ApiService.Session.Role switch
        {
            2 => new[] { "Duyuru Kanalı Oluştur", "Özel Mesaj Gönder" },
            3 => new[] { "Çalışma Odası Aç", "Özel Mesaj Gönder" },
            _ => new[] { "Özel Mesaj Gönder", "Derse Katıl" }
        };

        string action = await DisplayActionSheet(
            "Ne yapmak istersiniz?", "İptal", null, options);

        switch (action)
        {
            case "Özel Mesaj Gönder":
                await Navigation.PushAsync(new SearchUserPage());
                break;
            case "Derse Katıl":
                await Navigation.PushModalAsync(new JoinCoursePage());
                break;
            case "Çalışma Odası Aç":
            case "Duyuru Kanalı Oluştur":
                await Navigation.PushAsync(new CreateCoursePage());
                break;
        }
    }
}
