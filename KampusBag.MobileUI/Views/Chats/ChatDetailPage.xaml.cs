using System.Collections.ObjectModel;

namespace KampusBag.MobileUI.Views.Chats;

public partial class ChatDetailPage : ContentPage
{
    public ObservableCollection<string> Messages { get; set; }

    private readonly bool _isReadOnly;

    // GÜNCELLEME: chatName ve isReadOnly parametreleri eklendi
    public ChatDetailPage(
        bool isPrivateWithTeacher,
        string chatName = "Sohbet",
        bool isReadOnly = false)
    {
        InitializeComponent();

        Messages = new ObservableCollection<string>();
        _isReadOnly = isReadOnly;

        MessagesList.ItemsSource = Messages;
        Title = chatName;

        // Acil buton: Sadece Akademisyen ile özel sohbette göster
        EmergencyButton.IsVisible = isPrivateWithTeacher && !isReadOnly;

        // Salt okunur mod: Giriş alanını ve gönder butonunu kilitle
        if (isReadOnly)
        {
            MessageEntry.IsEnabled = false;
            MessageEntry.Placeholder = "Bu kanal sadece okunabilir 🔒";
            MessageEntry.BackgroundColor = Color.FromArgb("#F3F4F6");
        }
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        if (_isReadOnly) return;
        if (string.IsNullOrWhiteSpace(MessageEntry.Text)) return;

        Messages.Add(MessageEntry.Text);
        MessageEntry.Text = string.Empty;

        if (Messages.Count > 0)
            MessagesList.ScrollTo(Messages.Count - 1);
    }

    private async void OnEmergencyClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert(
            "🚨 Acil Durum",
            "Bu mesaj 1 acil hakkınızı tüketecektir. Emin misiniz?",
            "Evet, Gönder",
            "Vazgeç");

        if (!answer) return;

        string emergencyMsg = "🚨 ACİL: " +
            (string.IsNullOrWhiteSpace(MessageEntry.Text)
                ? "Acil durum bildirimi!"
                : MessageEntry.Text);

        Messages.Add(emergencyMsg);
        MessageEntry.Text = string.Empty;

        await DisplayAlert("✅ Gönderildi",
            "Acil mesajınız hocanın paneline iletildi.", "Tamam");
    }
}
