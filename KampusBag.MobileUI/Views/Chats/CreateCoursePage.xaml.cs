using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class CreateCoursePage : ContentPage
{
    public CreateCoursePage()
    {
        InitializeComponent();
        BindingContext = new CreateCourseViewModel(this);
    }
}
