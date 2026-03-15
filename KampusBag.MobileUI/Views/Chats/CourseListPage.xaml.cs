namespace KampusBag.MobileUI.Views.Chats;

public partial class CourseListPage : ContentPage
{
    public CourseListPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Herhangi bir dersin "Sohbete Git" butonuna basıldığında tetiklenir.
    /// </summary>
    private async void OnCourseClicked(object sender, EventArgs e)
    {
        // Butona tıklandığında hangi ders olduğunu anlamak için şimdilik basit bir mantık kuruyoruz.
        // İleride burası veritabanından gelen ID ile çalışacak.

        var button = sender as Button;
        if (button == null) return;

        // Dersin bir hoca ile olan özel sohbet olup olmadığını kontrol eden geçici bir mantık.
        // Proje gereği 7 ders grubunda hoca olduğu için true gönderiyoruz.
        bool hasTeacher = true;

        // Sohbet detay sayfasına yönlendiriyoruz
        // Not: ChatDetailPage constructor'ı bu bool değerini bekliyor olmalı.
        await Navigation.PushAsync(new ChatDetailPage(hasTeacher));
    }

    /// <summary>
    /// Yeni bir derse katılma butonuna basıldığında (Opsiyonel)
    /// </summary>
    private async void OnJoinNewCourseClicked(object sender, EventArgs e)
    {
        // Daha önce konuştuğumuz "Kod ile Derse Katıl" sayfasına yönlendirme yapılacak.
        // await Navigation.PushAsync(new JoinCoursePage());
    }
    // ChatListPage.xaml.cs içinde "+" butonuna basınca:
    private async void OnAddChatClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Yeni İşlem", "Vazgeç", null, "Koda Göre Derse Katıl");

        if (action == "Koda Göre Derse Katıl")
        {
            // Modal olarak sayfayı açıyoruz
            await Navigation.PushModalAsync(new JoinCoursePage());
        }
    }
}