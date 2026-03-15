namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatListPage : ContentPage
{
    public ChatListPage()
    {
        InitializeComponent();
    }

    private async void OnAddChatClicked(object sender, EventArgs e)
    {
        // "+" Butonuna basınca açılacak seçenekler menüsü
        string action = await DisplayActionSheet("Yeni Sohbet", "Vazgeç", null,
            "Koda Göre Derse Katıl", "Öğrenci No ile Sohbet Başlat");

        if (action == "Koda Göre Derse Katıl")
        {
            // Buraya ileride kod girme sayfasını bağlayacağız
            await DisplayAlert("Bilgi", "Kod ile katılma özelliği yakında eklenecek.", "Tamam");
        }
        else if (action == "Öğrenci No ile Sohbet Başlat")
        {
            // Buraya bireysel sohbet başlatma gelecek
        }
    }
}