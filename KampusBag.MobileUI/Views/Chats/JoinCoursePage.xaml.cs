namespace KampusBag.MobileUI.Views.Chats;

public partial class JoinCoursePage : ContentPage
{
    public JoinCoursePage()
    {
        InitializeComponent();
    }

    private async void OnJoinClicked(object sender, EventArgs e)
    {
        string code = CourseCodeEntry.Text?.ToUpper();

        if (string.IsNullOrEmpty(code) || code.Length < 6)
        {
            await DisplayAlert("Hata", "Lütfen 6 haneli geçerli bir kod giriniz.", "Tamam");
            return;
        }

        // Burada API'ye kod gönderilecek
        await DisplayAlert("Başarılı", $"{code} kodlu derse kaydınız yapıldı!", "Harika");

        // Modal'ı kapatıp geri dönüyoruz
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Hiçbir şey yapmadan modalı kapat
        await Navigation.PopModalAsync();
    }
}