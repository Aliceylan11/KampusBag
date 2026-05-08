using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class CourseListViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api = new();

    // ── Koleksiyonlar ─────────────────────────────────────────────────
    public ObservableCollection<CourseModel> Courses { get; } = new();
    private List<CourseModel> _allCourses = new();

    // ── State ─────────────────────────────────────────────────────────
    private bool _isLoading;
    private bool _hasError;
    private bool _isEmpty;
    private string _errorMessage = string.Empty;
    private string _searchText = string.Empty;

    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
    public bool HasError { get => _hasError; set => Set(ref _hasError, value); }
    public bool IsEmpty { get => _isEmpty; set => Set(ref _isEmpty, value); }
    public string ErrorMessage { get => _errorMessage; set => Set(ref _errorMessage, value); }

    public string SearchText
    {
        get => _searchText;
        set { Set(ref _searchText, value); FilterCourses(); }
    }

    // Akademisyen veya Temsilci veya Admin yeni ders açabilir
    public bool CanCreateCourse => ApiService.Session.Role is 2 or 3 or 4;

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }

    public CourseListViewModel()
    {
        LoadCommand = new Command(async () => await LoadCoursesAsync());
        RefreshCommand = new Command(async () => await LoadCoursesAsync());
    }

    // ════════════════════════════════════════════════════════════════
    // VERİ YÜKLEME
    // ════════════════════════════════════════════════════════════════
    public async Task LoadCoursesAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            var result = await _api.GetMyCoursesAsync(ApiService.Session.UserId);

            if (!result.Success)
            {
                HasError = true;
                ErrorMessage = result.Error;
                return;
            }

            _allCourses = result.Courses;
            FilterCourses();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dersler yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TEMSİLCİ ATAMA / GERI ALMA
    // Sadece Akademisyen (Role=2) ve Admin (Role=4) çağırabilir.
    // ════════════════════════════════════════════════════════════════
    public async Task<string> AssignRepresentativeAsync(
        Guid courseId, Guid studentId, bool revoke)
    {
        var (success, message) = await _api.AssignRepresentativeAsync(
            courseId, studentId, revoke);

        if (success)
            await LoadCoursesAsync(); // Listeyi güncelle

        return message;
    }

    /// <summary>
    /// Verilen dersin üye listesini getirir.
    /// Akademisyen, temsilci atamadan önce bu listeyi çeker.
    /// </summary>
    public async Task<(bool success, List<CourseMemberModel> members, string error)>
        GetCourseMembersAsync(Guid courseId)
        => await _api.GetCourseMembersAsync(courseId);

    // ════════════════════════════════════════════════════════════════
    // ARAMA FİLTRE
    // ════════════════════════════════════════════════════════════════
    private void FilterCourses()
    {
        var q = _searchText.ToLower().Trim();

        var filtered = string.IsNullOrEmpty(q)
            ? _allCourses
            : _allCourses.Where(c =>
                c.Name.ToLower().Contains(q) ||
                c.CourseCode.ToLower().Contains(q) ||
                c.AcademicName.ToLower().Contains(q));

        Courses.Clear();
        foreach (var c in filtered) Courses.Add(c);

        IsEmpty = Courses.Count == 0 && !IsLoading;
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
