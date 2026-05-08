using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class CourseDetailPage : ContentPage
{
    private readonly CourseDetailViewModel _vm;
    private readonly CourseModel _course;

    public CourseDetailPage(CourseModel course)
    {
        InitializeComponent();
        _course = course;
        _vm = new CourseDetailViewModel(this, course);
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadMembersAsync();
    }

    // ── Geri ─────────────────────────────────────────────────────────
    private async void OnBackClicked(object sender, EventArgs e)
        => await Navigation.PopAsync();

    // ── Sohbete Geç ──────────────────────────────────────────────────
    private async void OnOpenChatClicked(object sender, TappedEventArgs e)
    {
        // Okuma-yazma yetkisi:
        // Öğrenci + Resmi Kanal + Temsilci değil → sadece oku
        bool isReadOnly = _course.IsOfficial
            && ApiService.Session.Role == 1
            && !_course.IsRepresentative;

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: _course.Name,
            courseId: _course.Id,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: isReadOnly
        ));
    }
}
