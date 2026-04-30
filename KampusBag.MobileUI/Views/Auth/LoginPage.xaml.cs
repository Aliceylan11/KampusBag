using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnLoginSubmitClicked(object sender, EventArgs e)
    {
        // 1. Boş alan kontrolü
        if (string.IsNullOrWhiteSpace(IdentifierEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        // 2. Loading göstergesi
        var loginButton = sender as Button;
        if (loginButton != null)
        {
            loginButton.IsEnabled = false;
            loginButton.Text = "Giriş yapılıyor...";
        }

        try
        {
            var (success, message) = await _apiService.LoginAsync(
                IdentifierEntry.Text.Trim(),
                PasswordEntry.Text);

            if (success)
            {
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("Giriş Başarısız", message, "Tamam");
                PasswordEntry.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Beklenmeyen bir hata: {ex.Message}", "Tamam");
        }
        finally
        {
            if (loginButton != null)
            {
                loginButton.IsEnabled = true;
                loginButton.Text = "Giriş Yap";
            }
        }
    }

    // GÜNCELLEME: ForgotPasswordPage sayfasına yönlendirme
    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ForgotPasswordPage());
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
