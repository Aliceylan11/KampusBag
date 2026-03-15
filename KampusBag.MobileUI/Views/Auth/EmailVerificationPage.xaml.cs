namespace KampusBag.MobileUI.Views.Auth;

public partial class EmailVerificationPage : ContentPage
{
    public EmailVerificationPage()
    {
        InitializeComponent();
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        if (OtpEntry.Text?.Length == 6)
        {
            await DisplayAlert("Başarılı", "Hesabınız doğrulandı. Giriş yapabilirsiniz.", "Tamam");
            // Doğrulama başarılıysa giriş ekranına yönlendir
            await Navigation.PopToRootAsync();
        }
        else
        {
            await DisplayAlert("Hata", "Lütfen 6 haneli kodu eksiksiz giriniz.", "Tamam");
        }
    }

    private async void OnResendCodeClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Bilgi", "Yeni kod mail adresinize gönderildi.", "Tamam");
    }
}