using KampusBag.MobileUI.Services;
using KampusBag.Core.DTOs; // ForgotPasswordDto için gerekebilir

namespace KampusBag.MobileUI.Views.Auth;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly ApiService _apiService;

    public ForgotPasswordPage()
    {
        InitializeComponent();
        _apiService = new ApiService(); // API servis bağlantısı
        _apiService = new ApiService();
    }

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();

        // 1. Boş alan kontrolü
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Hata", "Lütfen kurumsal e-posta adresinizi girin.", "Tamam");
            await DisplayAlert("Hata", "Lütfen e-posta adresinizi girin.", "Tamam");
            return;
        }

        // 2. Okul maili format kontrolü (Domain doğrulaması)
        if (!email.EndsWith("@ogr.gumushane.edu.tr") && !email.EndsWith("@gumushane.edu.tr"))
        {
            await DisplayAlert("Geçersiz Mail", "Lütfen sadece Gümüşhane Üniversitesi mailinizi kullanın.", "Tamam");
            return;
        }

        // 3. Butonu kilitle ve loading göster
        // 2. Buton kilitleme (Çift tıklama önleme)
        SendCodeButton.IsEnabled = false;
        SendCodeButton.Text = "Kod Gönderiliyor...";
        SendCodeButton.Text = "Gönderiliyor...";

        try
        {
            // API'ye forgot-password isteği atıyoruz
            // 3. API Servis çağrısı
            var (success, message) = await _apiService.ForgotPasswordAsync(email);

            if (success)
            {
                await DisplayAlert("Başarılı", "Sıfırlama kodu mail adresinize gönderildi.", "Tamam");
                await DisplayAlert("Başarılı", "Sıfırlama kodu gönderildi.", "Tamam");

                // Kod başarıyla gittiyse, kullanıcıyı ResetPasswordPage'e yönlendiriyoruz
                // E-postayı yanımızda taşıyoruz ki bir sonraki sayfada tekrar yazmasın
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
            await DisplayAlert("Hata", $"Bir sorun oluştu: {ex.Message}", "Tamam");
            await DisplayAlert("Hata", $"Bağlantı hatası: {ex.Message}", "Tamam");
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