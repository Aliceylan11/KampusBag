using KampusBag.Core.DTOs;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Auth;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;

    public RegisterPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnRegisterSubmitClicked(object sender, EventArgs e)
    {
        string studentNumber = StudentNumberEntry.Text;
        string fullName = FullNameEntry.Text;
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        // 1. Boş Alan Kontrolü
        if (string.IsNullOrWhiteSpace(studentNumber) || string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları eksiksiz doldurun.", "Tamam");
            return;
        }

        // 2. Şifre Uyuşmazlığı Kontrolü
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert("Hata", "Girdiğiniz şifreler birbiriyle uyuşmuyor. Lütfen kontrol edin.", "Tamam");
            return;
        }

        // Çift tıklamayı önlemek için butonu kilitliyoruz
        var clickedButton = sender as Button;
        if (clickedButton != null) clickedButton.IsEnabled = false;

        try
        {
            // 3. Verileri Modele (DTO) Doldurma
            var registerDto = new UserRegisterDto
            {
                RegistrationNumber = studentNumber,
                FullName = fullName,
                Email = email,
                Password = password
            };

            // 4. API İsteği
            bool isSuccess = await _apiService.RegisterAsync(registerDto);

            if (isSuccess)
            {
                await DisplayAlert("Başarılı", "Kayıt yapıldı! Mailinize gelen kodu girin.", "Tamam");

                // Bir önceki adımda yazdığımız sayfaya parametre ile geçiş yapıyoruz
                await Navigation.PushAsync(new EmailVerificationPage(email));
            }
            else
            {
                await DisplayAlert("Kayıt Hatası", "Sunucuya ulaşılamadı veya bu kullanıcı zaten kayıtlı.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Kayıt sırasında bir sorun oluştu: {ex.Message}", "Tamam");
        }
        finally
        {
            // İşlem bittiğinde butonu tekrar aktif et
            if (clickedButton != null) clickedButton.IsEnabled = true;
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Zaten bir Login sayfasından buraya geldiysek geri dönmesini sağlar
        await Navigation.PopAsync();
    }
}