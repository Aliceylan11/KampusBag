namespace KampusBag.MobileUI.Views.Profile;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    // Karanlık Mod Anahtarı (Zaten yapmıştık ama teyit edelim)
    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    // GÜVENLİ ÇIKIŞ: İşte o meşhur metod
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Çıkış", "Oturumu kapatmak istediğinize emin misiniz?", "Evet", "Hayır");

        if (answer)
        {
            // Shell'den tamamen çıkıp Giriş ekranına (MainPage) kökten reset atıyoruz
            Application.Current.MainPage = new NavigationPage(new Views.MainPage());
        }
    }
}