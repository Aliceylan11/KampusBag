using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _apiService;

    public ForgotPasswordPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();

        // 1. Boş alan kontrolü
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Hata", "Lütfen kurumsal e-posta adresinizi girin.", "Tamam");
            return;
        }

        // 2. Okul maili format kontrolü
        if (!email.EndsWith("@ogr.gumushane.edu.tr", StringComparison.OrdinalIgnoreCase) &&
            !email.EndsWith("@gumushane.edu.tr", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert(
                "Geçersiz E-posta",
                "Lütfen yalnızca Gümüşhane Üniversitesi kurumsal e-postanızı kullanın.",
                "Tamam");
            return;
        }

        // 3. Butonu kilitle
        SendCodeButton.IsEnabled = false;
        SendCodeButton.Text = "Kod Gönderiliyor...";

        try
        {
            var (success, message) = await _apiService.ForgotPasswordAsync(email);

            if (success)
            {
                await DisplayAlert(
                    "Başarılı",
                    "Şifre sıfırlama kodu e-posta adresinize gönderildi.",
                    "Tamam");

                // Sonraki sayfaya e-postayı taşı
                await Navigation.PushAsync(new ResetPasswordPage(email));
            }
            else
            {
                await DisplayAlert("Hata", message, "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Bağlantı Hatası", $"Bir sorun oluştu: {ex.Message}", "Tamam");
        }
        finally
        {
            SendCodeButton.IsEnabled = true;
            SendCodeButton.Text = "Kod Gönder";
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
        => await Navigation.PopAsync();
}
