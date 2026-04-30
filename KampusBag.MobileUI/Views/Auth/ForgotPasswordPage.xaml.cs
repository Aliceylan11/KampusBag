using KampusBag.MobileUI.Services;
using KampusBag.Core.DTOs; // ForgotPasswordDto için gerekebilir

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
            await DisplayAlert("Hata", "Lütfen e-posta adresinizi girin.", "Tamam");
            return;
        }

        // 2. Buton kilitleme (Çift tıklama önleme)
        SendCodeButton.IsEnabled = false;
        SendCodeButton.Text = "Gönderiliyor...";

        try
        {
            // 3. API Servis çağrısı
            var (success, message) = await _apiService.ForgotPasswordAsync(email);

            if (success)
            {
                await DisplayAlert("Başarılı", "Sıfırlama kodu gönderildi.", "Tamam");

                // 4. ResetPasswordPage'e yönlendirme[cite: 2]
                // Eğer ResetPasswordPage henüz yoksa burası kırmızı çizgi olur!
                await Navigation.PushAsync(new ResetPasswordPage(email));
            }
            else
            {
                await DisplayAlert("Hata", message, "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Bağlantı hatası: {ex.Message}", "Tamam");
        }
        finally
        {
            SendCodeButton.IsEnabled = true;
            SendCodeButton.Text = "Kod Gönder";
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}