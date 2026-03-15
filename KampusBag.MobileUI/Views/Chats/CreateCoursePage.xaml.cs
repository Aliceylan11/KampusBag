namespace KampusBag.MobileUI.Views.Chats;

public partial class CreateCoursePage : ContentPage
{
    public CreateCoursePage()
    {
        InitializeComponent();
    }

    private async void OnCreateCourseClicked(object sender, EventArgs e)
    {
        // Basit validasyon
        if (string.IsNullOrWhiteSpace(CourseNameEntry.Text) || CourseCodeEntry.Text?.Length < 6)
        {
            await DisplayAlert("Hata", "Lütfen tüm alanları eksiksiz ve kodu 6 hane olacak şekilde doldurun.", "Tamam");
            return;
        }

        string courseName = CourseNameEntry.Text;
        string courseCode = CourseCodeEntry.Text.ToUpper();

        // Burada veri tabanına kayıt işlemi yapılacak (Backend bağlandığında)
        await DisplayAlert("Başarılı", $"{courseName} dersi {courseCode} koduyla oluşturuldu.", "Tamam");

        // İşlem bittikten sonra bir önceki sayfaya dön
        await Navigation.PopAsync();
    }
}