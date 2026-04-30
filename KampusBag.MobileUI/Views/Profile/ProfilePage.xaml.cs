using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Profile;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        // Session'dan kullanıcı bilgilerini çek
        var fullName = ApiService.Session.FullName;
        var email = ApiService.Session.Email;
        var role = ApiService.Session.Role;

        // Ad Soyad
        FullNameLabel.Text = string.IsNullOrEmpty(fullName) ? "Kullanıcı" : fullName;

        // Avatar: İsmin baş harfi
        AvatarLabel.Text = string.IsNullOrEmpty(fullName)
            ? "?"
            : fullName.Trim()[0].ToString().ToUpper();

        // E-posta
        EmailLabel.Text = string.IsNullOrEmpty(email) ? "—" : email;

        // Öğrenci No / Sicil No — e-postadan @ öncesi
        if (!string.IsNullOrEmpty(email) && email.Contains("@"))
        {
            var prefix = email.Split('@')[0];

            // Akademisyen için sicil, öğrenci için numara etiketi
            RegistrationLabel.Text = role == 2 ? "Sicil No" : "Öğrenci No";
            RegistrationNumberLabel.Text = prefix;
        }
        else
        {
            RegistrationLabel.Text = "Kayıt No";
            RegistrationNumberLabel.Text = "—";
        }

        // Rol gösterimi
        switch (role)
        {
            case 1:
                RoleLabel.Text = "👨‍🎓  Öğrenci";
                RoleDetailLabel.Text = "Öğrenci";
                break;
            case 2:
                RoleLabel.Text = "👨‍🏫  Akademisyen";
                RoleDetailLabel.Text = "Akademisyen";
                break;
            case 3:
                RoleLabel.Text = "🏅  Temsilci";
                RoleDetailLabel.Text = "Sınıf Temsilcisi";
                break;
            default:
                RoleLabel.Text = "Kullanıcı";
                RoleDetailLabel.Text = "—";
                break;
        }
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert(
            "Çıkış Yap",
            "Oturumunuzu kapatmak istediğinize emin misiniz?",
            "Evet, Çık",
            "Hayır");

        if (!answer) return;

        // Session'ı temizle
        ApiService.Session.Clear();

        // Navigasyon stack'ini sıfırla ve MainPage'e dön
        Application.Current.MainPage = new NavigationPage(new Views.MainPage());
    }
}
