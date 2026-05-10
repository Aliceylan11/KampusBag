using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatDetailPage : ContentPage
{
    private readonly ChatDetailViewModel _vm;

    public ChatDetailPage(
        string chatName,
        Guid? courseId,
        Guid? otherUserId,
        bool isPrivateWithTeacher,
        bool isReadOnly = false)
    {
        InitializeComponent();

        _vm = new ChatDetailViewModel(this)
        {
            ChatName = chatName,
            CourseId = courseId,
            OtherUserId = otherUserId,
            IsPrivateWithTeacher = isPrivateWithTeacher,
            IsReadOnly = isReadOnly
        };

        BindingContext = _vm;
        ChatTitleLabel.Text = chatName;
    }

    // Geriye uyumluluk (eski çağrılar için)
    public ChatDetailPage(bool isPrivateWithTeacher)
        : this("Sohbet", null, null, isPrivateWithTeacher) { }

    // ════════════════════════════════════════════════════════════════
    // SAYFA GÖRÜNDÜĞÜNDESignalR başlat + geçmiş yükle
    // ════════════════════════════════════════════════════════════════
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();   // SignalR + geçmiş
    }

    // ════════════════════════════════════════════════════════════════
    // SAYFA KAPANIRKEN → odadan ayrıl
    // ════════════════════════════════════════════════════════════════
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _vm.CleanupAsync();      // oda ayrılma + bağlantı kapat
    }
}
