using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Profile;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        // Cihazın mevcut temasına göre switch'i ayarla
        DarkModeSwitch.IsToggled = Application.Current.RequestedTheme == AppTheme.Dark;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        var fullName = ApiService.Session.FullName;
        var email = ApiService.Session.Email;
        var role = ApiService.Session.Role;

        FullNameLabel.Text = string.IsNullOrEmpty(fullName) ? "Yönetici" : fullName;
        AvatarLabel.Text = string.IsNullOrEmpty(fullName) ? "A" : fullName.Trim()[0].ToString().ToUpper();
        EmailLabel.Text = email ?? "—";

        if (role == 4) // Hotmail Admin Kontrolü
        {
            RoleLabel.Text = "👑  Sistem Yöneticisi";
            RoleDetailLabel.Text = "SuperAdmin / Geliştirici";
            RegistrationLabel.Text = "Yönetici Kimliği";
            RegistrationNumberLabel.Text = "ADMIN-001";
            EmergencyRightsLabel.Text = "∞";
        }
        else
        {
            if (!string.IsNullOrEmpty(email) && email.Contains("@"))
            {
                var prefix = email.Split('@')[0];
                RegistrationLabel.Text = role == 2 ? "Sicil No" : "Öğrenci No";
                RegistrationNumberLabel.Text = prefix;
            }

            switch (role)
            {
                case 1:
                    RoleLabel.Text = "👨‍🎓  Öğrenci";
                    RoleDetailLabel.Text = "Öğrenci";
                    EmergencyRightsLabel.Text = "3";
                    break;
                case 2:
                    RoleLabel.Text = "👨‍🏫  Akademisyen";
                    RoleDetailLabel.Text = "Akademisyen";
                    EmergencyRightsLabel.Text = "∞";
                    break;
                case 3:
                    RoleLabel.Text = "👥  Temsilci";
                    RoleDetailLabel.Text = "Temsilci";
                    EmergencyRightsLabel.Text = "3";
                    break;
            }
        }
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Çıkış", "Oturumu kapatıyorsunuz?", "Evet", "Hayır");
        if (answer)
        {
            ApiService.Session.Clear();
            Application.Current.MainPage = new NavigationPage(new Views.Auth.LoginPage());
        }
    }
}