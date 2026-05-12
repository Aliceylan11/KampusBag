using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

/// <summary>
/// #7 DÜZELTME NOTU — Fiziksel Android'de Tap Çalışmıyordu:
/// CollectionView DataTemplate içinde outer Grid'e eklenen TapGestureRecognizer,
/// Frame/Border'ın touch event'i tüketmesi nedeniyle ARM cihazlarda tetiklenmiyordu.
/// Çözüm: TapGestureRecognizer'ı Frame'in kendisine taşı (ChatListPage.xaml'de).
///
/// Değişiklik: XAML'daki her CollectionView item'ında:
///   ESKİ: &lt;Grid&gt;&lt;Frame&gt;...&lt;/Frame&gt;&lt;Grid.GestureRecognizers&gt;...
///   YENİ: &lt;Frame&gt;&lt;Frame.GestureRecognizers&gt;...&lt;/Frame.GestureRecognizers&gt;...
/// </summary>
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

    // ════════════════════════════════════════════════════════════════
    // NAVIGASYON HANDLER'LARI
    // #7: Her handler CourseId/OtherUserId null kontrolü yapar
    // ════════════════════════════════════════════════════════════════

    /// <summary>Resmi Kanal tıklama — Frame.GestureRecognizers'dan tetiklenir.</summary>
    private async void OnOfficialChannelTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        // #7 GUARD
        if (!item.CourseId.HasValue)
        {
            await DisplayAlert("Hata", "Bu kanala erişilemiyor (CourseId eksik).", "Tamam");
            return;
        }

        bool canWrite = ApiService.Session.Role is 2 or 3 or 4
                     || item.IsUserRepresentative;

        await NavigateToChatAsync(
            chatName: item.DisplayName,
            courseId: item.CourseId,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: item.IsLocked && !canWrite);
    }

    /// <summary>Çalışma Odası tıklama — Frame.GestureRecognizers'dan tetiklenir.</summary>
    private async void OnStudyRoomTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        // #7 GUARD
        if (!item.CourseId.HasValue)
        {
            await DisplayAlert("Hata", "Bu odaya erişilemiyor (CourseId eksik).", "Tamam");
            return;
        }

        await NavigateToChatAsync(
            chatName: item.DisplayName,
            courseId: item.CourseId,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: false);
    }

    /// <summary>Özel Mesaj tıklama — Sessiz mod kontrolü ile.</summary>
    private async void OnPrivateChatTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatSummaryModel item) return;

        // #7 GUARD
        if (!item.OtherUserId.HasValue)
        {
            await DisplayAlert("Hata", "Bu sohbete erişilemiyor (UserId eksik).", "Tamam");
            return;
        }

        if (item.IsSilentMode)
        {
            bool proceed = await DisplayAlert(
                "🌙 Sessiz Mod Aktif",
                $"{item.DisplayName} şu an sessiz mod saatlerinde (17:00+).\n"
                + "Bildirim gönderilmeyecek. Devam edilsin mi?",
                "Devam Et", "İptal");

            if (!proceed) return;
        }

        await NavigateToChatAsync(
            chatName: item.DisplayName,
            courseId: null,
            otherUserId: item.OtherUserId,
            isPrivateWithTeacher: item.OtherUserRole == 2,
            isReadOnly: false);
    }

    // ════════════════════════════════════════════════════════════════
    // ORTAK NAVİGASYON — try-catch ile crash önleme
    // ════════════════════════════════════════════════════════════════
    private async Task NavigateToChatAsync(
        string chatName,
        Guid? courseId,
        Guid? otherUserId,
        bool isPrivateWithTeacher,
        bool isReadOnly)
    {
        try
        {
            await Navigation.PushAsync(new ChatDetailPage(
                chatName,
                courseId,
                otherUserId,
                isPrivateWithTeacher,
                isReadOnly));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatListPage] Navigation hatası: {ex}");
            await DisplayAlert("Hata", $"Sayfa açılamadı: {ex.Message}", "Tamam");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // DİĞER BUTONLAR
    // ════════════════════════════════════════════════════════════════

    private async void OnSearchUserClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new SearchUserPage());

    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());

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