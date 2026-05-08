using KampusBag.MobileUI.Models;
using KampusBag.MobileUI.Services;
using KampusBag.MobileUI.ViewModels;

namespace KampusBag.MobileUI.Views.Chats;

public partial class CourseListPage : ContentPage
{
    private CourseListViewModel _vm => (CourseListViewModel)BindingContext;

    public CourseListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm != null)
            await _vm.LoadCoursesAsync();
    }

    // ── Ders kartına tıklama ──────────────────────────────────────────
    private async void OnCourseTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not CourseModel course) return;

        // Okuma-yazma yetkisi belirleme:
        // Öğrenci (Role=1) → çalışma odalarına yazabilir, resmi kanallara yazamaz
        // Temsilci         → her iki kanala da yazabilir
        // Hoca/Admin       → her şeye yazabilir
        bool isReadOnly = course.IsOfficial
            && ApiService.Session.Role == 1
            && !course.IsRepresentative;

        await Navigation.PushAsync(new ChatDetailPage(
            chatName: course.Name,
            courseId: course.Id,
            otherUserId: null,
            isPrivateWithTeacher: false,
            isReadOnly: isReadOnly
        ));
    }

    // ── Ders kartına uzun basma (Akademisyen: temsilci atama) ─────────
    /// <summary>
    /// Akademisyen bir ders kartına uzun basarsa temsilci atama ekranı açılır.
    /// XAML'da bu handler'ı kullanmak için LongPressGestureRecognizer
    /// veya SwipeGestureRecognizer ekleyin. Şimdilik "Seçenekler" butonu
    /// aracılığıyla da tetiklenebilir.
    /// </summary>
    public async void OnCourseLongPressed(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not CourseModel course) return;

        // Sadece Akademisyen ve Admin temsilci atayabilir
        if (ApiService.Session.Role is not (2 or 4)) return;

        await ShowRepresentativeManagementAsync(course);
    }

    // ── Temsilci yönetim akışı ────────────────────────────────────────
    public async Task ShowRepresentativeManagementAsync(CourseModel course)
    {
        // Üye listesini getir
        var (success, members, error) = await _vm.GetCourseMembersAsync(course.Id);

        if (!success)
        {
            await DisplayAlert("Hata", error, "Tamam");
            return;
        }

        // Sadece öğrencileri ve mevcut temsilcileri listele
        var students = members
            .Where(m => m.Role is 1 or 3)
            .ToList();

        if (!students.Any())
        {
            await DisplayAlert("Bilgi", "Bu derste henüz öğrenci üye yok.", "Tamam");
            return;
        }

        // Kullanıcıya öğrenci listesini göster
        var options = students
            .Select(s => $"{s.FullName}{(s.IsRepresentative ? " ✅ (Temsilci)" : "")}")
            .ToArray();

        string? selected = await DisplayActionSheet(
            $"'{course.Name}' — Temsilci Yönetimi",
            "İptal",
            null,
            options);

        if (selected == null || selected == "İptal") return;

        // Seçilen öğrenciyi bul
        var target = students[Array.IndexOf(options, selected)];

        bool revoke = target.IsRepresentative;
        string action = revoke ? "temsilcilikten almak" : "temsilci yapmak";

        bool confirm = await DisplayAlert(
            "Onay",
            $"{target.FullName} adlı öğrenciyi {action} istediğinize emin misiniz?",
            "Evet",
            "İptal");

        if (!confirm) return;

        string result = await _vm.AssignRepresentativeAsync(
            course.Id, target.UserId, revoke);

        await DisplayAlert(
            revoke ? "Temsilcilik Alındı" : "Temsilci Atandı",
            result,
            "Tamam");
    }

    // ── Toolbar butonları ─────────────────────────────────────────────
    private async void OnJoinCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new JoinCoursePage());

    private async void OnCreateCourseClicked(object sender, EventArgs e)
        => await Navigation.PushModalAsync(new CreateCoursePage());
}
