using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class JoinCoursePage : ContentPage
{
    public JoinCoursePage()
    {
        InitializeComponent();
        BindingContext = new JoinCourseViewModel(this);
        CodeEntry.Focus();
    }
}
