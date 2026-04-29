using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class EmailVerificationPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly string _userEmail;

    // Constructor'ı (Yapıcı metodu) email parametresi alacak şekilde güncelledik
    public EmailVerificationPage(string email)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _userEmail = email; // Kayıt sayfasından gelen e-postayı değişkene atıyoruz
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        // Butonu en başta güvenli bir şekilde tanımlıyoruz
        var clickedButton = sender as Button;

        if (OtpEntry.Text?.Length == 6)
        {
            // Çift tıklamayı önlemek için butonu kilitliyoruz
            if (clickedButton != null)
            {
                clickedButton.IsEnabled = false;
                clickedButton.Text = "Doğrulanıyor..."; // Loading göstergesi
            }

            try
            {
                var result = await _apiService.VerifyEmailAsync(_userEmail, OtpEntry.Text);

                if (result.Contains("başarıyla") || result.ToLower().Contains("success"))
                {
                    await DisplayAlert("Başarılı", "Hesabınız doğrulandı. Giriş yapabilirsiniz.", "Tamam");

                    // GÜNCELLEME: Login sayfasına yönlendir
                    // Navigation stack'i temizleyip Login sayfasına git
                    await Navigation.PopToRootAsync(); // Ana sayfaya dön
                }
                else
                {
                    await DisplayAlert("Hata", result, "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Bir sorun oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                // İşlem bitince butonu tekrar aktif ediyoruz
                if (clickedButton != null)
                {
                    clickedButton.IsEnabled = true;
                    clickedButton.Text = "Doğrula ve Devam Et";
                }
            }
        }
        else
        {
            await DisplayAlert("Hata", "Lütfen 6 haneli kodu eksiksiz giriniz.", "Tamam");
        }
    }

    private async void OnResendCodeClicked(object sender, EventArgs e)
    {
        // Şimdilik sadece uyarı veriyor, ileride buraya da API isteği eklenebilir
        await DisplayAlert("Bilgi", "Yeni kod mail adresinize gönderildi.", "Tamam");
    }
}