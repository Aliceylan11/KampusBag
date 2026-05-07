using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace KampusBag.MobileUI.ViewModels;

public class ChatListViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService = new();

    // ── Koleksiyonlar ─────────────────────────────────────────────────
    public ObservableCollection<ChatSummaryModel> OfficialChannels { get; } = new();
    public ObservableCollection<ChatSummaryModel> StudyRooms { get; } = new();
    public ObservableCollection<ChatSummaryModel> PrivateMessages { get; } = new();

    // ── Ham listeler (arama için) ─────────────────────────────────────
    private List<ChatSummaryModel> _allOfficials = new();
    private List<ChatSummaryModel> _allStudy = new();
    private List<ChatSummaryModel> _allPrivate = new();

    // ── State ─────────────────────────────────────────────────────────
    private bool _isLoading;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private string _searchQuery = string.Empty;
    private bool _isEmpty;

    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
    public bool HasError { get => _hasError; set => Set(ref _hasError, value); }
    public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }
    public bool IsEmpty { get => _isEmpty; set => Set(ref _isEmpty, value); }

    // ── Sayaçlar ──────────────────────────────────────────────────────
    public int TotalUnread
        => OfficialChannels.Sum(c => c.UnreadCount)
         + StudyRooms.Sum(c => c.UnreadCount)
         + PrivateMessages.Sum(c => c.UnreadCount);

    // ── Komutlar ──────────────────────────────────────────────────────
    public ICommand LoadCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }

    public ChatListViewModel()
    {
        LoadCommand = new Command(async () => await LoadChatsAsync());
        SearchCommand = new Command<string>(OnSearch);
        RefreshCommand = new Command(async () => await LoadChatsAsync());
    }

    // ── Veri Yükleme ─────────────────────────────────────────────────
    public async Task LoadChatsAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            var result = await _apiService.GetChatListAsync(ApiService.Session.UserId);

            if (!result.Success)
            {
                HasError = true;
                ErrorMessage = result.Error;
                return;
            } 
            // Ham listelere kaydet
            _allOfficials = result.OfficialChannels;
            _allStudy = result.StudyRooms;
            _allPrivate = result.PrivateMessages;

            // UI koleksiyonlarını güncelle
            FillCollection(OfficialChannels, _allOfficials);
            FillCollection(StudyRooms, _allStudy);
            FillCollection(PrivateMessages, _allPrivate);

            UpdateIsEmpty();
            OnPropertyChanged(nameof(TotalUnread));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Veri yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Arama ─────────────────────────────────────────────────────────
    private void OnSearch(string query)
    {
        _searchQuery = query?.ToLower().Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(_searchQuery))
        {
            FillCollection(OfficialChannels, _allOfficials);
            FillCollection(StudyRooms, _allStudy);
            FillCollection(PrivateMessages, _allPrivate);
        }
        else
        {
            FillCollection(OfficialChannels,
                _allOfficials.Where(c => Matches(c, _searchQuery)));
            FillCollection(StudyRooms,
                _allStudy.Where(c => Matches(c, _searchQuery)));
            FillCollection(PrivateMessages,
                _allPrivate.Where(c => Matches(c, _searchQuery)));
        }

        UpdateIsEmpty();
    }

    private static bool Matches(ChatSummaryModel c, string q)
        => c.DisplayName.ToLower().Contains(q)
        || c.LastMessage.ToLower().Contains(q);

    // ── Yardımcılar ───────────────────────────────────────────────────
    private static void FillCollection(
        ObservableCollection<ChatSummaryModel> col,
        IEnumerable<ChatSummaryModel> source)
    {
        col.Clear();
        foreach (var item in source) col.Add(item);
    }

    private void UpdateIsEmpty()
    {
        IsEmpty = OfficialChannels.Count == 0
               && StudyRooms.Count == 0
               && PrivateMessages.Count == 0;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
