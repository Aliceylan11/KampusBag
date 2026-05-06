using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class SearchUserViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api = new();

    // ── State ─────────────────────────────────────────────────────────
    private string _searchQuery = string.Empty;
    private bool _isSearching;
    private bool _hasSearched;
    private bool _isEmpty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public ObservableCollection<UserSearchModel> Results { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            Set(ref _searchQuery, value);
            OnPropertyChanged(nameof(CanSearch));
            // Anlık arama: 3+ karakter olunca otomatik tetikle
            if (value.Length >= 3) SearchCommand.Execute(null);
            else if (string.IsNullOrEmpty(value)) Results.Clear();
        }
    }

    public bool IsSearching { get => _isSearching; set => Set(ref _isSearching, value); }
    public bool HasSearched { get => _hasSearched; set => Set(ref _hasSearched, value); }
    public bool IsEmpty { get => _isEmpty; set => Set(ref _isEmpty, value); }
    public bool HasError { get => _hasError; set => Set(ref _hasError, value); }
    public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }
    public bool CanSearch => SearchQuery.Length >= 2 && !IsSearching;

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand SearchCommand { get; }

    public SearchUserViewModel()
    {
        SearchCommand = new Command(async () => await SearchAsync());
    }

    // ════════════════════════════════════════════════════════════════
    // ARAMA
    // ════════════════════════════════════════════════════════════════
    private CancellationTokenSource? _cts;   // Debounce için

    private async Task SearchAsync()
    {
        if (!CanSearch) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            await Task.Delay(400, token);
            if (token.IsCancellationRequested) return;

            IsSearching = true;
            HasError = false;

            // 🛡️ GÜVENLİK KONTROLÜ: Girdi tamamen sayı mı?
            bool isSearchingByNumber = int.TryParse(SearchQuery, out _);

            var (success, users, error) = await _api.SearchUsersAsync(SearchQuery);

            if (token.IsCancellationRequested) return;

            Results.Clear();
            HasSearched = true;

            if (!success)
            {
                HasError = true; ErrorMessage = error; IsEmpty = true;
                return;
            }

            // 🛡️ VERİ FİLTRELEME (Post-Processing)
            var filteredUsers = users.Where(u => {
                if (u.Role == 4) return false; // Role 4 (Yönetici) kesinlikle gösterme
                if (isSearchingByNumber)
                {
                    // Sadece numara ile arama yapılıyorsa ÖĞRENCİLERİ (Role 1) ve TEMSİLCİLERİ (Role 3) getir
                    return u.Role == 1 || u.Role == 3;
                }
                else
                {
                    // Metin ile arama yapılıyorsa sadece HOCALARI (Role 2) getir
                    return u.Role == 2;
                }
            }).ToList();

            foreach (var u in filteredUsers)
            {
                // 🛡️ HASSAS VERİ GİZLEME: Hoca ise numarasını kesinlikle null yap
                if (u.Role == 2)
                {
                    u.RegistrationNumber = null; // UI'da görünmesini engelle
                }
                Results.Add(u);
            }

            IsEmpty = Results.Count == 0;
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Arama başarısız: {ex.Message}";
            IsEmpty = true;
        }
        finally
        {
            if (!token.IsCancellationRequested) IsSearching = false;
        }
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
    {
        if (EqualityComparer<T>.Default.Equals(f, v)) return;
        f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
