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
        // Sayfa her göründüğünde dersleri yenile
        if (_vm != null)
            await _vm.LoadCoursesAsync();
    }

    private async void OnCourseTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not CourseModel course) return;

        // Öğrenci ise ve temsilci değilse salt okunur moduna al 
        bool isReadOnly = !course.IsRepresentative && ApiService.Session.Role == 1;

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: course.Name,
            courseId: course.Id,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: isReadOnly
        ));
    }

    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());
    private async void OnCreateCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new CreateCoursePage());

}