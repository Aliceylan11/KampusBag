using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
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

    // ── Ders kartına tıklama → CourseDetailPage ───────────────────────
    // Akademisyen hem üyeleri yönetir hem sohbete geçer.
    // Öğrenci de üye listesini görür, sohbete oradan geçer.
    private async void OnCourseTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not CourseModel course) return;
        await Navigation.PushAsync(new CourseDetailPage(course));  // ← bu satır
    }

    // ── Toolbar butonları ─────────────────────────────────────────────
    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());

    private async void OnCreateCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new CreateCoursePage());
}
