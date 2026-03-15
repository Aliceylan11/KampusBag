using System.Collections.ObjectModel;

namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatDetailPage : ContentPage
{
    // Mesajları tutacak olan dinamik liste. 
    // Bu listeye ekleme yapıldığında CollectionView otomatik olarak güncellenir.
    public ObservableCollection<string> Messages { get; set; }

    public ChatDetailPage(bool isPrivateWithTeacher)
    {
        InitializeComponent();

        // Listeyi başlatıyoruz
        Messages = new ObservableCollection<string>();

        // XAML tarafındaki MessagesList isimli CollectionView'a bu listeyi bağlıyoruz
        MessagesList.ItemsSource = Messages;

        // Eğer bu sohbet bir Hoca ile ise kırmızı butonu göster
        EmergencyButton.IsVisible = isPrivateWithTeacher;
    }

    // Mesaj Gönderme Butonu (➤)
    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        // Boş mesaj gönderilmesini engelle
        if (string.IsNullOrWhiteSpace(MessageEntry.Text)) return;

        // Mesajı listeye ekle (Ekrana anında düşer)
        Messages.Add(MessageEntry.Text);

        // Gönderdikten sonra kutuyu temizle
        MessageEntry.Text = string.Empty;

        // (Opsiyonel) Mesaj gönderildiğinde listenin en altına kaydır
        if (Messages.Count > 0)
        {
            MessagesList.ScrollTo(Messages.Count - 1);
        }
    }

    // ACİL DURUM BUTONU (🚨)
    private async void OnEmergencyClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Acil Durum",
            "Bu mesaj 1 hakkınızı tüketecektir. Emin misiniz?", "Evet", "Vazgeç");

        if (answer)
        {
            // Mesaj kutusundaki metni acil durum mesajı olarak ekle
            string emergencyMsg = "🚨 ACİL: " + (string.IsNullOrWhiteSpace(MessageEntry.Text) ? "Acil durum bildirimi!" : MessageEntry.Text);
            Messages.Add(emergencyMsg);

            await DisplayAlert("Bilgi", "Acil durum mesajı hoca paneline iletildi!", "Tamam");
            MessageEntry.Text = string.Empty;
        }
    }
}