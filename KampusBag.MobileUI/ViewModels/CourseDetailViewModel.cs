using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class CourseDetailViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api = new();
    private readonly Page _page;

    // ── Ders Bilgisi ──────────────────────────────────────────────────
    public CourseModel Course { get; }

    // ── Üye Listesi ───────────────────────────────────────────────────
    // CourseMemberModel kullanıyoruz: IsRepresentative alanı var, UserSearchModel'de yok.
    public ObservableCollection<CourseMemberModel> Members { get; } = new();
    private List<CourseMemberModel> _allMembers = new();

    // ── State ─────────────────────────────────────────────────────────
    private bool _isLoading;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private string _searchText = string.Empty;

    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
    public bool HasError { get => _hasError; set => Set(ref _hasError, value); }
    public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }

    public string SearchText
    {
        get => _searchText;
        set { Set(ref _searchText, value); FilterMembers(); }
    }

    // ── Yetki ─────────────────────────────────────────────────────────
    /// <summary>
    /// Sadece Akademisyen (Role=2) ve Admin (Role=4) temsilci atayabilir.
    /// Koşul: Session'daki kullanıcı bu dersin sahibi olmalı (server tarafı da doğrular).
    /// </summary>
    public bool CanManageRepresentatives => ApiService.Session.Role is 2 or 4;

    // ── Hesaplananlar (XAML Binding) ──────────────────────────────────
    public string MemberCountText => $"{Members.Count} üye";
    public string CourseTypeLabel => Course.IsOfficial ? "🏛️ Resmi Kanal" : "📚 Çalışma Odası";
    public string CourseTypeColor => Course.IsOfficial ? "#1B305E" : "#059669";
    public string CourseHeaderIcon => Course.IsOfficial ? "🏛️" : "📚";

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand LoadMembersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AssignRepresentativeCommand { get; }

    public CourseDetailViewModel(Page page, CourseModel course)
    {
        _page = page;
        Course = course;

        LoadMembersCommand = new Command(async () => await LoadMembersAsync());
        RefreshCommand = new Command(async () => await LoadMembersAsync());
        AssignRepresentativeCommand = new Command<CourseMemberModel>(
            async member => await ToggleRepresentativeAsync(member),
            member => CanManageRepresentatives && member != null);
    }

    // ════════════════════════════════════════════════════════════════
    // ÜYELERİ YÜKLE
    // ════════════════════════════════════════════════════════════════
    public async Task LoadMembersAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            var (success, members, error) = await _api.GetCourseMembersAsync(Course.Id);

            if (!success)
            {
                HasError = true;
                ErrorMessage = error;
                return;
            }

            _allMembers = members;
            FilterMembers();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Üyeler yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TEMSİLCİ ATA / GERİ AL
    // ════════════════════════════════════════════════════════════════
    private async Task ToggleRepresentativeAsync(CourseMemberModel member)
    {
        if (member == null || !CanManageRepresentatives) return;

        // Sadece öğrenciler (Role=1) ve mevcut temsilciler (Role=3 veya IsRepresentative=true)
        // hedef alınabilir. Hoca veya admin seçilemez.
        if (member.Role is not (1 or 3) && !member.IsRepresentative)
        {
            await _page.DisplayAlert("Uyarı",
                "Sadece öğrenciler temsilci yapılabilir.", "Tamam");
            return;
        }

        bool isRevoke = member.IsRepresentative;
        string action = isRevoke ? "temsilcilikten almak" : "temsilci yapmak";

        bool confirm = await _page.DisplayAlert(
            "Temsilci Yönetimi",
            $"'{member.FullName}' adlı öğrenciyi {action} istediğinize emin misiniz?",
            "Evet", "İptal");

        if (!confirm) return;

        var (success, message) = await _api.AssignRepresentativeAsync(
            Course.Id, member.UserId, isRevoke);

        await _page.DisplayAlert(
            success
                ? (isRevoke ? "✅ Temsilcilik Alındı" : "✅ Temsilci Atandı")
                : "❌ Hata",
            message,
            "Tamam");

        if (success)
            await LoadMembersAsync(); // Listeyi yenile
    }

    // ════════════════════════════════════════════════════════════════
    // FİLTRELEME
    // ════════════════════════════════════════════════════════════════
    private void FilterMembers()
    {
        var q = _searchText.ToLower().Trim();

        var filtered = string.IsNullOrEmpty(q)
            ? _allMembers
            : _allMembers.Where(m =>
                m.FullName.ToLower().Contains(q) ||
                m.Email.ToLower().Contains(q));

        Members.Clear();
        foreach (var m in filtered) Members.Add(m);

        OnPropertyChanged(nameof(MemberCountText));
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