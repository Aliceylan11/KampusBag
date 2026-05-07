using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KampusBag.MobileUI.Services;

namespace KampusBag.MobileUI.ViewModels;

public class JoinCourseViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api = new();
    private readonly Page _page;

    // ── State ─────────────────────────────────────────────────────────
    private string _courseCode = string.Empty;
    private bool _isLoading;

    public string CourseCode
    {
        get => _courseCode;
        set { Set(ref _courseCode, value.ToUpper()); OnPropertyChanged(nameof(CanJoin)); (JoinCommand as Command)?.ChangeCanExecute(); }
    }

    public bool IsLoading { get => _isLoading; set { Set(ref _isLoading, value); OnPropertyChanged(nameof(CanJoin)); (JoinCommand as Command)?.ChangeCanExecute(); } }
    public bool CanJoin => CourseCode.Length == 6 && !IsLoading;

    // ── Komutlar ─────────────────────────────────────────────────────
    public ICommand JoinCommand { get; }
    public ICommand CancelCommand { get; }

    public JoinCourseViewModel(Page page)
    {
        _page = page;
        JoinCommand = new Command(async () => await JoinAsync(), () => CanJoin);
        CancelCommand = new Command(async () => await page.Navigation.PopModalAsync());
    }

    private async Task JoinAsync()
    {
        IsLoading = true;

        try
        {
            var (success, message) = await _api.JoinCourseAsync(CourseCode);

            if (success)
            {
                await _page.DisplayAlert("🎉 Katıldın!", message, "Tamam");
                await _page.Navigation.PopModalAsync();
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
    {
        if (EqualityComparer<T>.Default.Equals(f, v)) return;
        f = v; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
