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
        // Kutulardan verileri tam olarak XAML'daki isimleriyle alıyoruz
        string studentNumber = StudentNumberEntry.Text;
        string fullName = FullNameEntry.Text;
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;

        // Boşluk Kontrolü
        if (string.IsNullOrWhiteSpace(studentNumber) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları eksiksiz doldurun.", "Tamam");
            return;
        }

        // Verileri paketliyoruz
        var registerDto = new UserRegisterDto
        {
            RegistrationNumber = studentNumber,
            FullName = fullName,
            Email = email,
            Password = password
        };

        // API'ye gönderiyoruz!
        bool isSuccess = await _apiService.RegisterAsync(registerDto);

        if (isSuccess)
        {
            await DisplayAlert("Başarılı", "Hesabınız oluşturuldu. Lütfen doğrulama adımına geçin.", "Harika!");
            await Navigation.PushAsync(new EmailVerificationPage());
        }
        else
        {
            await DisplayAlert("Kayıt Hatası", "Sunucuya ulaşılamadı veya bu kullanıcı zaten kayıtlı.", "Tamam");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}