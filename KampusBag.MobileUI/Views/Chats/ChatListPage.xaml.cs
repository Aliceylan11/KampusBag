using KampusBag.MobileUI.Services;
using System.Collections.ObjectModel;

namespace KampusBag.MobileUI.Views.Chats;

// ════════════════════════════════════════════════════
// CHAT ITEM MODEL — Tüm kategoriler için ortak model
// ════════════════════════════════════════════════════
public class ChatItem
{
    // Temel bilgiler
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Icon { get; set; } = "💬";

    // Grup özellikleri
    public bool IsLocked { get; set; }   // Resmi Kanal kilitli mi?
    public string MemberCountText { get; set; } = string.Empty;

    // Özel mesaj özellikleri
    public bool IsAcademic { get; set; }   // Karşı taraf akademisyen mi?
    public bool IsOnline { get; set; }   // Çevrimiçi mi?
    public bool IsSilentMode { get; set; }   // 17:00 sonrası sessiz mod
    public bool HasEmergency { get; set; }   // Acil durum mesajı var mı?

    // Okunmamış
    public bool HasUnread { get; set; }
    public string UnreadCount { get; set; } = string.Empty;

    // Avatar (özel mesajlar için)
    public string AvatarInitial { get; set; } = "?";
    public string AvatarColor { get; set; } = "#1B305E";

    // Sessiz mod border rengi
    public string SilentModeBorderColor => IsSilentMode ? "#FECACA" : "#E5E7EB";
}

// ════════════════════════════════════════════════════
// CHAT CATEGORY — Kategori ayırıcısı
// ════════════════════════════════════════════════════
public enum ChatCategory
{
    OfficialChannel,
    StudyRoom,
    PrivateMessage
}

// ════════════════════════════════════════════════════
// CHATLISTPAGE
// ════════════════════════════════════════════════════
public partial class ChatListPage : ContentPage
{
    // Tüm listeler
    private ObservableCollection<ChatItem> _officialChannels = new();
    private ObservableCollection<ChatItem> _studyRooms = new();
    private ObservableCollection<ChatItem> _privateMessages = new();

    // Arama filtrelemesi için ham listeler
    private List<ChatItem> _allOfficials = new();
    private List<ChatItem> _allStudy = new();
    private List<ChatItem> _allPrivate = new();

    // Mevcut kullanıcı rolü
    private int _userRole => ApiService.Session.Role;

    public ChatListPage()
    {
        InitializeComponent();
        WireCollections();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAllChats();
    }

    // ────────────────────────────────────────
    // CollectionView'ları bağla
    // ────────────────────────────────────────
    private void WireCollections()
    {
        OfficialChannelsList.ItemsSource = _officialChannels;
        StudyRoomsList.ItemsSource = _studyRooms;
        PrivateMessagesList.ItemsSource = _privateMessages;
    }

    // ────────────────────────────────────────
    // TÜM VERİYİ YÜKLE
    // ────────────────────────────────────────
    private void LoadAllChats()
    {
        LoadOfficialChannels();
        LoadStudyRooms();
        LoadPrivateMessages();
        UpdateCountBadges();
        UpdateEmptyState();
    }

    // ════════════════════════════════════════
    // KATEGORİ A — RESMİ KANALLAR
    // Kilitli: sadece Role 2 (Akademisyen) ve
    //          Role 3 (Temsilci) mesaj yazabilir
    // ════════════════════════════════════════
    private void LoadOfficialChannels()
    {
        // Kullanıcı yalnızca Öğrenci (1) ise kanallar kilitli görünür
        bool isLocked = _userRole == 1;

        _allOfficials = new List<ChatItem>
        {
            new()
            {
                Name        = "📢 Bölüm Duyuruları",
                LastMessage = "Proje teslim tarihi: 20 Mayıs 2026",
                TimeText    = "10:30",
                Icon        = "🏛️",
                IsLocked    = isLocked,
                HasUnread   = true,
                UnreadCount = "3"
            },
            new()
            {
                Name        = "📋 Sınav Programları",
                LastMessage = "Vize takvimi güncellenmiştir.",
                TimeText    = "Dün",
                Icon        = "🗓️",
                IsLocked    = isLocked,
                HasUnread   = false
            },
            new()
            {
                Name        = "🏫 Dekanlık Bildirimleri",
                LastMessage = "Yeni akademik takvim yayınlandı.",
                TimeText    = "Çrş",
                Icon        = "📜",
                IsLocked    = isLocked,
                HasUnread   = true,
                UnreadCount = "1"
            }
        };

        _officialChannels.Clear();
        foreach (var item in _allOfficials)
            _officialChannels.Add(item);
    }

    // ════════════════════════════════════════
    // KATEGORİ B — ÇALIŞMA ODALARI
    // Herkes okuyabilir; herkes yazabilir
    // Sadece Temsilci (3) yeni oda açabilir
    // ════════════════════════════════════════
    private void LoadStudyRooms()
    {
        _allStudy = new List<ChatItem>
        {
            new()
            {
                Name            = "Algoritma Çalışma Grubu",
                LastMessage     = "Yarın saat 14:00'de buluşuyoruz!",
                TimeText        = "09:15",
                Icon            = "📐",
                MemberCountText = "24 üye",
                HasUnread       = true,
                UnreadCount     = "7"
            },
            new()
            {
                Name            = "Proje Ekibi — Bitirme",
                LastMessage     = "API dokümantasyonunu paylaştım.",
                TimeText        = "Dün",
                Icon            = "💻",
                MemberCountText = "6 üye",
                HasUnread       = false
            },
            new()
            {
                Name            = "Veri Yapıları Soru-Cevap",
                LastMessage     = "Stack ve Queue farkı nedir tam olarak?",
                TimeText        = "Sal",
                Icon            = "📚",
                MemberCountText = "38 üye",
                HasUnread       = true,
                UnreadCount     = "12"
            }
        };

        _studyRooms.Clear();
        foreach (var item in _allStudy)
            _studyRooms.Add(item);
    }

    // ════════════════════════════════════════
    // KATEGORİ C — ÖZEL MESAJLAR
    // Sessiz mod: Alıcı Akademisyen + saat 17:00 sonrası
    // ════════════════════════════════════════
    private void LoadPrivateMessages()
    {
        bool isSilentHour = DateTime.Now.Hour >= 17;

        // Statik demo verisi (ileride API'den gelecek)
        var rawData = new List<(string name, string preview, string time,
                                string color, bool isAcademic, bool isOnline,
                                bool hasEmergency)>
        {
            ("Nihat Özdemir",   "Ödev hakkında görüşebilir miyiz?",
             "14:22",  "#1B305E", true,  false, false),

            ("Elif Yılmaz",     "Proje toplantısını erteledik mi?",
             "Dün",    "#7C3AED", false, true,  false),

            ("Ahmet Kaya",      "🚨 Acil: Not girişi yapılmadı!",
             "10:05",  "#DC2626", false, false, true),

            ("Dr. Selin Çelik", "Vize sorusu hakkında bilgi verdim.",
             "Pzt",    "#059669", true,  false, false),

            ("Mehmet Demir",    "Kitap paylaşımı için teşekkürler",
             "Paz",    "#F59E0B", false, false, false),
        };

        _allPrivate = rawData.Select(d => new ChatItem
        {
            Name = d.name,
            LastMessage = d.preview,
            TimeText = d.time,
            AvatarInitial = d.name[0].ToString().ToUpper(),
            AvatarColor = d.color,
            IsAcademic = d.isAcademic,
            IsOnline = d.isOnline,
            HasEmergency = d.hasEmergency,

            // Sessiz mod: Akademisyen + 17:00 sonrası
            IsSilentMode = d.isAcademic && isSilentHour,

            HasUnread = d.hasEmergency,
            UnreadCount = d.hasEmergency ? "!" : string.Empty,

            // Icon gerek yok, avatar kullanıyoruz
            Icon = d.isAcademic ? "👨‍🏫" : "👤"
        }).ToList();

        _privateMessages.Clear();
        foreach (var item in _allPrivate)
            _privateMessages.Add(item);
    }

    // ────────────────────────────────────────
    // SAYAÇ ROZETLER
    // ────────────────────────────────────────
    private void UpdateCountBadges()
    {
        OfficialCountLabel.Text = _officialChannels.Count.ToString();
        StudyCountLabel.Text = _studyRooms.Count.ToString();
        PrivateCountLabel.Text = _privateMessages.Count.ToString();
    }

    // ────────────────────────────────────────
    // BOŞ DURUM
    // ────────────────────────────────────────
    private void UpdateEmptyState()
    {
        bool isEmpty = _officialChannels.Count == 0
                    && _studyRooms.Count == 0
                    && _privateMessages.Count == 0;
        EmptyStateView.IsVisible = isEmpty;
    }

    // ════════════════════════════════════════
    // ARAMA — Anlık filtreleme
    // ════════════════════════════════════════
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.ToLower().Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(query))
        {
            // Aramayı sıfırla
            RefreshList(_officialChannels, _allOfficials);
            RefreshList(_studyRooms, _allStudy);
            RefreshList(_privateMessages, _allPrivate);
        }
        else
        {
            RefreshList(_officialChannels,
                _allOfficials.Where(x => x.Name.ToLower().Contains(query)
                                      || x.LastMessage.ToLower().Contains(query)));
            RefreshList(_studyRooms,
                _allStudy.Where(x => x.Name.ToLower().Contains(query)
                                  || x.LastMessage.ToLower().Contains(query)));
            RefreshList(_privateMessages,
                _allPrivate.Where(x => x.Name.ToLower().Contains(query)
                                    || x.LastMessage.ToLower().Contains(query)));
        }

        UpdateCountBadges();
        UpdateEmptyState();
    }

    private static void RefreshList(ObservableCollection<ChatItem> collection,
                                    IEnumerable<ChatItem> source)
    {
        collection.Clear();
        foreach (var item in source)
            collection.Add(item);
    }

    // ════════════════════════════════════════
    // NAVİGASYON — Sohbete Git
    // ════════════════════════════════════════
    private async void OnChannelTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatItem item) return;

        // Kilitli kanal: Öğrenci okuyabilir ama mesaj atamaz
        bool canWrite = _userRole is 2 or 3; // Akademisyen veya Temsilci

        await Navigation.PushAsync(
            new ChatDetailPage(
                isPrivateWithTeacher: false,
                chatName: item.Name,
                isReadOnly: item.IsLocked && !canWrite
            )
        );
    }

    private async void OnPrivateChatTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ChatItem item) return;

        // Sessiz mod uyarısı
        if (item.IsSilentMode)
        {
            bool proceed = await DisplayAlert(
                "🔴 Sessiz Mod Aktif",
                $"{item.Name} şu an sessiz mod saatlerinde (17:00 sonrası). " +
                "Acil mesaj hakkınızı kullanmak ister misiniz?",
                "Acil Mesaj Gönder",
                "İptal");

            if (!proceed) return;
        }

        await Navigation.PushAsync(
            new ChatDetailPage(
                isPrivateWithTeacher: item.IsAcademic,
                chatName: item.Name,
                isReadOnly: false
            )
        );
    }

    // ════════════════════════════════════════
    // YENİ SOHBET — Rol bazlı seçenekler
    // ════════════════════════════════════════
    private async void OnNewChatClicked(object sender, EventArgs e)
    {
        // Rol'e göre farklı seçenekler
        var options = _userRole switch
        {
            2 => new[] { "Duyuru Kanalı Oluştur", "Özel Mesaj Gönder" },
            3 => new[] { "Çalışma Odası Aç", "Özel Mesaj Gönder" },
            _ => new[] { "Özel Mesaj Gönder", "Derse Katıl" }
        };

        string action = await DisplayActionSheet(
            "Ne yapmak istersiniz?", "İptal", null, options);

        switch (action)
        {
            case "Özel Mesaj Gönder":
                await Navigation.PushAsync(new SearchUserPage());
                break;
            case "Derse Katıl":
                await Navigation.PushModalAsync(new JoinCoursePage());
                break;
            case "Çalışma Odası Aç":
            case "Duyuru Kanalı Oluştur":
                await Navigation.PushAsync(new CreateCoursePage());
                break;
        }
    }
}
