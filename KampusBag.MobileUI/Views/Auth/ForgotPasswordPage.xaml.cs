using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _apiService;

    public ForgotPasswordPage()
    {
        InitializeComponent();
        _apiService = new ApiService(); // API servis bağlantısı
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

        // 2. Okul maili format kontrolü (Domain doğrulaması)
        if (!email.EndsWith("@ogr.gumushane.edu.tr") && !email.EndsWith("@gumushane.edu.tr"))
        {
            await DisplayAlert("Geçersiz Mail", "Lütfen sadece Gümüşhane Üniversitesi mailinizi kullanın.", "Tamam");
            return;
        }

        // 3. Butonu kilitle ve loading göster
        SendCodeButton.IsEnabled = false;
        SendCodeButton.Text = "Kod Gönderiliyor...";

        try
        {
            // API'ye forgot-password isteği atıyoruz
            var (success, message) = await _apiService.ForgotPasswordAsync(email);

            if (success)
            {
                await DisplayAlert("Başarılı", "Sıfırlama kodu mail adresinize gönderildi.", "Tamam");

                // Kod başarıyla gittiyse, kullanıcıyı ResetPasswordPage'e yönlendiriyoruz
                // E-postayı yanımızda taşıyoruz ki bir sonraki sayfada tekrar yazmasın
                await Navigation.PushAsync(new ResetPasswordPage(email));
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
            // İşlem bittiğinde butonu eski haline getir
            SendCodeButton.IsEnabled = true;
            SendCodeButton.Text = "Kod Gönder";
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        // Login sayfasına geri dönüş (Navigation stack'ten çıkar)
        await Navigation.PopAsync();
    }
}