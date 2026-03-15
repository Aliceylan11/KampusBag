namespace KampusBag.MobileUI.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginSubmitClicked(object sender, EventArgs e)
    {
        // 1. Burada normalde backend kontrolü yapılır. 
        // Şimdilik 'boş değilse gir' mantığı kuralım.
        if (!string.IsNullOrEmpty(IdentifierEntry.Text) && !string.IsNullOrEmpty(PasswordEntry.Text))
        {
            // 2. BAĞLANTI NOKTASI: 
            // Mevcut MainPage'i çöpe atıp yerine AppShell'i (Tabbar'lı yapıyı) koyuyoruz.
            Application.Current.MainPage = new AppShell();

            // Not: 'new NavigationPage' dememize gerek yok, Shell zaten kendi içinde 
            // gelişmiş bir navigasyon barındırır.
        }
        else
        {
            await DisplayAlert("Hata", "Lütfen bilgilerinizi eksiksiz girin.", "Tamam");
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Bir önceki sayfaya (eğer Register ise) dönmek daha mantıklı olabilir:
        // await Navigation.PopAsync();
        // Ama doğrudan gitmek istersen bu da çalışır:
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Bilgi", "Şifre sıfırlama servisi yakında aktif edilecektir.", "Tamam");
    }
}