using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatDetailPage : ContentPage
{
    private readonly ChatDetailViewModel? _vm;

    public ChatDetailPage(
        string chatName,
        Guid? courseId,
        Guid? otherUserId,
        bool isPrivateWithTeacher,
        bool isReadOnly = false)
    {
        InitializeComponent();

        // #7 GUARD: Her iki parametre de null ise VM oluşturma
        if (courseId == null && otherUserId == null)
        {
            Console.WriteLine("[ChatDetailPage] courseId ve otherUserId her ikisi de null.");
            return;
        }

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_vm == null)
        {
            await DisplayAlert("Hata", "Sohbet bilgisi eksik.", "Tamam");
            await Navigation.PopAsync();
            return;
        }

        try
        {
            await _vm.InitializeAsync();
        }
        catch (Exception ex)
        {
            // #7: async void içinde yakalanmayan exception Android'de uygulamayı çökertir
            Console.WriteLine($"[ChatDetailPage] InitializeAsync hata: {ex}");
            await DisplayAlert("Bağlantı Hatası", $"Sohbet açılamadı:\n{ex.Message}", "Tamam");
            await Navigation.PopAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vm != null)
            await _vm.CleanupAsync();
    }
}