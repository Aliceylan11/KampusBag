namespace KampusBag.MobileUI.Views.Chats;

public partial class SearchUserPage : ContentPage
{
    public SearchUserPage()
    {
        InitializeComponent();
    }

    // Klavye üzerindeki "Ara" butonuna basıldığında
    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        string searchText = UserSearchBar.Text;

        if (string.IsNullOrWhiteSpace(searchText)) return;

        // Burada backend araması yapılacak. Şimdilik simüle ediyoruz.
        await DisplayAlert("Arama", $"{searchText} numaralı öğrenci aranıyor...", "Tamam");
    }

    // Listeden birine mesaj at dediğimizde
    private async void OnStartChatClicked(object sender, EventArgs e)
    {
        // Bireysel sohbette hoca olmadığı için 'false' gönderiyoruz
        // Böylece 🚨 butonu gizli kalacak.
        await Navigation.PushAsync(new ChatDetailPage(false));
    }
}