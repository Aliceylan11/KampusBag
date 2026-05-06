using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class CreateCourseViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api = new();
    private readonly Page _page;

    // ── Alanlar ────────────────────────────────────────────────────────
    private string _courseName = string.Empty;
    private string _courseCode = string.Empty;
    private bool _isLoading;

    public string CourseName
    {
        get => _courseName;
        set { Set(ref _courseName, value); OnPropertyChanged(nameof(CanCreate)); OnPropertyChanged(nameof(AvatarInitial)); }
    }

    public string CourseCode
    {
        get => _courseCode;
        set { Set(ref _courseCode, value.ToUpper()); OnPropertyChanged(nameof(CanCreate)); OnPropertyChanged(nameof(CodeLength)); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { Set(ref _isLoading, value); OnPropertyChanged(nameof(CanCreate)); }
    }

    // ── Computed ──────────────────────────────────────────────────────
    public bool CanCreate => CourseName.Length >= 3 && CourseCode.Length == 6 && !IsLoading;
    public string CodeLength => $"{CourseCode.Length}/6";
    public string AvatarInitial => string.IsNullOrEmpty(CourseName) ? "?" : CourseName[0].ToString().ToUpper();

    // Akademisyen mi Temsilci mi? — Oluşturma türünü belirler
    public bool IsAcademic => ApiService.Session.Role == 2;
    public string PageTitle => IsAcademic ? "Resmi Kanal Oluştur" : "Çalışma Odası Aç";
    public string TypeDescription => IsAcademic
        ? "Sadece siz mesaj gönderebilirsiniz."
        : "Tüm üyeler mesaj gönderebilir.";
    public string TypeIcon => IsAcademic ? "🏛️" : "📚";
    public string TypeColor => IsAcademic ? "#1B305E" : "#059669";

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand CreateCommand { get; }
    public ICommand GenerateCodeCommand { get; }
    public ICommand CancelCommand { get; }

    public CreateCourseViewModel(Page page)
    {
        _page = page;
        CreateCommand = new Command(async () => await CreateAsync());
        GenerateCodeCommand = new Command(GenerateRandomCode);
        CancelCommand = new Command(async () => await page.Navigation.PopAsync());
    }

    // ════════════════════════════════════════════════════════════════
    // DERS OLUŞTUR
    // ════════════════════════════════════════════════════════════════
    private async Task CreateAsync()
    {
        if (!CanCreate) return;

        IsLoading = true;

        try
        {
            var (success, message, courseId) = await _api.CreateCourseAsync(
                CourseName.Trim(), CourseCode);

            if (success)
            {
                await _page.DisplayAlert(
                    "✅ Oluşturuldu!",
                    $"{CourseName} başarıyla oluşturuldu.\n\n" +
                    $"Katılım Kodu: {CourseCode}\n" +
                    "Bu kodu öğrencilerle paylaşın.",
                    "Harika!");

                await _page.Navigation.PopAsync();
            }
            else
            {
                await _page.DisplayAlert("Hata", message, "Tamam");
            }
        }
        catch (Exception ex)
        {
            await _page.DisplayAlert("Hata", $"İşlem başarısız: {ex.Message}", "Tamam");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // RASTGELE KOD ÜRET
    // ════════════════════════════════════════════════════════════════
    private void GenerateRandomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        CourseCode = new string(Enumerable.Range(0, 6)
            .Select(_ => chars[rng.Next(chars.Length)])
            .ToArray());
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
