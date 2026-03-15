using KampusBag.MobileUI.Views;
namespace KampusBag.MobileUI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Buradaki "Views.MainPage" kısmı bizim yaptığımız sayfayı işaret etmeli
        MainPage = new NavigationPage(new  MainPage());
    }
}