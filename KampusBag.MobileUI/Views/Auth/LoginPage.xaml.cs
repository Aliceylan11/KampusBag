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

        // 2. Butonu devre dışı bırak (çift tıklama önleme)
        var loginButton = sender as Button;
        if (loginButton != null)
        {
            loginButton.IsEnabled = false;
            loginButton.Text = "Giriş yapılıyor..."; // Loading göstergesi
        }

        try
        {
            // 3. API'ye login isteği gönder
            var (success, message) = await _apiService.LoginAsync(
                IdentifierEntry.Text.Trim(),
                PasswordEntry.Text
            );

            if (success)
            {
                // 4. Başarılı giriş - Ana sayfaya yönlendir
                await DisplayAlert("Başarılı", message, "Tamam");

                // Shell navigation ile ana sayfaya geçiş
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                // 5. Hatalı giriş - Kullanıcıya bilgi ver
                await DisplayAlert("Giriş Hatası", message, "Tamam");

                // Şifre alanını temizle (güvenlik)
                PasswordEntry.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Beklenmeyen bir hata oluştu: {ex.Message}", "Tamam");
        }
        finally
        {
            // 6. Butonu tekrar aktif et
            if (loginButton != null)
            {
                loginButton.IsEnabled = true;
                loginButton.Text = "Giriş Yap";
            }
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Kayıt sayfasına yönlendir
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Bilgi", "Şifre sıfırlama servisi yakında aktif edilecektir.", "Tamam");
    }
}