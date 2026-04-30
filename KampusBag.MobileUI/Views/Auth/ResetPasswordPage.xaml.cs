using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class ResetPasswordPage : ContentPage
{
    private readonly string _email;
    private readonly ApiService _apiService;

    // Constructor: Önceki sayfadan gelen email bilgisini parametre olarak alıyoruz
    public ResetPasswordPage(string email)
    {
        InitializeComponent();
        _email = email;
        _apiService = new ApiService();

        // Opsiyonel: Kullanıcıya hangi mail için işlem yaptığını hatırlatmak istersen
        // EmailLabel.Text = $"{email} adresi için yeni şifre belirleyin.";
    }

    private async void OnResetPasswordClicked(object sender, EventArgs e)
    {
        var code = CodeEntry.Text?.Trim();
        var newPassword = NewPasswordEntry.Text;

        // 1. Validasyonlar
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları doldurun.", "Tamam");
            return;
        }

        if (newPassword.Length < 6)
        {
            await DisplayAlert("Hata", "Yeni şifreniz en az 6 karakter olmalıdır.", "Tamam");
            return;
        }

        // 2. Yükleniyor durumu
        ResetButton.IsEnabled = false;
        ResetButton.Text = "Şifre Güncelleniyor...";

        try
        {
            // 3. ApiService üzerinden ResetPasswordDto gönderimi
            var (success, message) = await _apiService.ResetPasswordAsync(_email, code, newPassword);

            if (success)
            {
                await DisplayAlert("Başarılı", "Şifreniz başarıyla güncellendi. Giriş yapabilirsiniz.", "Tamam");

                // 4. Tüm sayfaları temizle ve Login (MainPage) sayfasına dön
                Application.Current.MainPage = new NavigationPage(new Views.MainPage());
            }
            else
            {
                await DisplayAlert("Hata", message, "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Bir sorun oluştu: {ex.Message}", "Tamam");
        }
        finally
        {
            ResetButton.IsEnabled = true;
            ResetButton.Text = "Şifreyi Güncelle";
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        // Direkt giriş ekranına dön
        await Navigation.PopToRootAsync();
    }
}