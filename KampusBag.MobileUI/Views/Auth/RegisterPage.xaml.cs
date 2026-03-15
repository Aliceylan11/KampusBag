namespace KampusBag.MobileUI.Views.Auth;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterSubmitClicked(object sender, EventArgs e)
    {
        // Örnek: Kullanıcının girdiği emaili almak istersen artık şöyle yapabilirsin:
        // string email = EmailEntry.Text;

        // Şimdilik doğrudan doğrulama sayfasına yönlendiriyoruz.
        await Navigation.PushAsync(new EmailVerificationPage());
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Zaten üye ise Giriş sayfasına (Navigation stack'te bir önceki sayfa) geri döner
        await Navigation.PopAsync();
    }
}