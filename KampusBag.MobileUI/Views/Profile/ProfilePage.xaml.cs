using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.Views.Profile;

public partial class ProfilePage : ContentPage
{
    private readonly ApiService _api = new();

    public ProfilePage()
    {
        InitializeComponent();
        DarkModeSwitch.IsToggled = Application.Current.RequestedTheme == AppTheme.Dark;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileDataAsync();
    }

    private async Task LoadProfileDataAsync()
    {
        try
        {
            // API'den en güncel kullanıcı verisini çekiyoruz (örneğin, toplam kurs sayısı, mesaj sayısı gibi dinamik veriler için)
            var response = await _api.GetProfileAsync(ApiService.Session.UserId);

            if (response.success && response.user != null)
            {
                var user = response.user;

                // 1. İsim ve Avatar
                FullNameLabel.Text = user.FullName ?? "İsimsiz Kullanıcı";
                AvatarLabel.Text = string.IsNullOrEmpty(user.FullName) ? "?" : user.FullName.Trim()[0].ToString().ToUpper();
                EmailLabel.Text = user.Email ?? "—";

                // 2. RegistrationNumber (PostgreSQL tablosundaki gerçek veri)
                RegistrationNumberLabel.Text = user.RegistrationNumber ?? "—";

                // 3. İstatistikler (API'den gelen dinamik sayılar)
                CourseCountLabel.Text = user.TotalCourses.ToString();
                MessageCountLabel.Text = user.TotalMessages.ToString();

                // 4. Rol ve Yetki Mantığı
                UpdateRoleUI(user.Role);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Profil bilgileri yüklenemedi.", "Tamam");
        }
    }

    private void UpdateRoleUI(int role)
    {
        switch (role)
        {
            case 4: // Admin
                RoleLabel.Text = "👑  Sistem Yöneticisi";
                RoleDetailLabel.Text = "SuperAdmin / Geliştirici";
                RegistrationLabel.Text = "Yönetici Kimliği";
                EmergencyRightsLabel.Text = "∞";
                break;
            case 2: // Akademisyen
                RoleLabel.Text = "👨‍🏫  Akademisyen";
                RoleDetailLabel.Text = "Öğretim Görevlisi";
                RegistrationLabel.Text = "Sicil No";
                EmergencyRightsLabel.Text = "∞";
                break;
            case 3: // Temsilci
                RoleLabel.Text = "👥  Sınıf Temsilcisi";
                RoleDetailLabel.Text = "Öğrenci Temsilcisi";
                RegistrationLabel.Text = "Öğrenci No";
                EmergencyRightsLabel.Text = "3";
                break;
            default: // Öğrenci
                RoleLabel.Text = "👨‍🎓  Öğrenci";
                RoleDetailLabel.Text = "Lisans/Önlisans";
                RegistrationLabel.Text = "Öğrenci No";
                EmergencyRightsLabel.Text = "3";
                break;
        }
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        if (await DisplayAlert("Çıkış", "Oturumu kapatmak istediğinize emin misiniz?", "Evet", "Hayır"))
        {
            ApiService.Session.Clear();
            Application.Current.MainPage = new NavigationPage(new Views.Auth.LoginPage());
        }
    }
}