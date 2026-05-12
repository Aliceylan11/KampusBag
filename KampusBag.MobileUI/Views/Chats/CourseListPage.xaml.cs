using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class CourseListPage : ContentPage
{
    private CourseListViewModel _vm => (CourseListViewModel)BindingContext;

    public CourseListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm != null)
            await _vm.LoadCoursesAsync();
    }

    // ── Ders kartına tıklama ──────────────────────────────────────────
    // ★★★ KRİTİK: Bu satır CourseDetailPage'e gitmeli (üye listesi sayfası).
    //          Eğer ChatDetailPage'e gidiyorsa, üyeler ekranı hiç açılmaz.
    private async void OnCourseTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not CourseModel course) return;

        try
        {
            await Navigation.PushAsync(new CourseDetailPage(course));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Ders detayı açılamadı: {ex.Message}", "Tamam");
        }
    }

    // ── Toolbar butonları ─────────────────────────────────────────────
    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());

    private async void OnCreateCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new CreateCoursePage());
}
