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
        Title = chatName;
        ChatTitleLabel.Text = chatName;
    } 

    public ChatDetailPage(bool isPrivateWithTeacher) : this("Bilinmeyen Sohbet", null, null, isPrivateWithTeacher)
    {
        // Bu, eski çağrıları kurtaracaktır.
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadHistoryAsync();

        // Listeyi en alta kaydır
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_vm.Messages.Count > 0)
            MessagesList.ScrollTo(_vm.Messages.Last(), animate: false);
    }
}
