using KampusBag.MobileUI.Views.Auth;
namespace KampusBag.MobileUI.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Kullanıcıyı Giriş Sayfasına gönderir
        await Navigation.PushAsync(new LoginPage());
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Kullanıcıyı Kayıt Sayfasına gönderir
        await Navigation.PushAsync(new RegisterPage());
    }
}