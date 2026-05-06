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

    // Eski çağrılarla (bool) geriye uyumluluk
    public ChatDetailPage(bool isPrivateWithTeacher)
        : this("Sohbet", null, null, isPrivateWithTeacher) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadHistoryAsync();
    }
}
