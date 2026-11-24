using System.Collections.ObjectModel;
using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

public partial class BirdStrikesPage : ContentPage
{
    private readonly BusinessLogic _businessLogic;
    private bool _filtersApplied;

    public ObservableCollection<BirdStrikeReport> Reports { get; } = new();
    public string StatusMessage { get; private set; } = "";

    public BirdStrikesPage(BusinessLogic businessLogic)
    {
        InitializeComponent();
        _businessLogic = businessLogic;
        BindingContext = this;
        StartDate.Date = DateTime.Today.AddYears(-100);
        EndDate.Date = DateTime.Today;
        StartTime.Time = TimeSpan.Zero;
        EndTime.Time = new TimeSpan(23, 59, 59);
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        var email = EmailInput.Text?.Trim();
        var password = PasswordInput.Text;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlertAsync("Sign in required", "Enter your email and password.", "OK");
            return;
        }

        try
        {
            await _businessLogic.SignInAsync(email, password);
            SignInPanel.IsVisible = false;
            ReportsPanel.IsVisible = true;
            RefreshButton.IsVisible = true;
            FilterButton.IsVisible = true;
            await LoadReportsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Unable to sign in", ex.Message, "OK");
        }
    }

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        FilterPanel.IsVisible = !FilterPanel.IsVisible;
        FilterButton.Text = FilterPanel.IsVisible ? "Close filters" : "Filter";
    }

    private void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        StartDate.Date = DateTime.Today.AddYears(-100);
        EndDate.Date = DateTime.Today;
        StartTime.Time = TimeSpan.Zero;
        EndTime.Time = new TimeSpan(23, 59, 59);
        ReporterFilter.Text = string.Empty;
        NotesFilter.Text = string.Empty;
        _filtersApplied = false;
    }

    private async void OnApplyFiltersClicked(object? sender, EventArgs e)
    {
        FilterPanel.IsVisible = false;
        FilterButton.Text = "Filter";
        _filtersApplied = true;
        await LoadReportsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadReportsAsync();

    private async Task LoadReportsAsync()
    {
        StatusMessage = "Loading reports…";
        OnPropertyChanged(nameof(StatusMessage));
        try
        {
            DateTime? start = _filtersApplied
                ? (StartDate.Date ?? DateTime.Today) + (StartTime.Time ?? TimeSpan.Zero)
                : null;
            DateTime? end = _filtersApplied
                ? (EndDate.Date ?? DateTime.Today) + (EndTime.Time ?? new TimeSpan(23, 59, 59))
                : null;
            var results = await _businessLogic.GetBirdStrikeSubmissionsAsync(
                start, end, ReporterFilter.Text, NotesFilter.Text);

            Reports.Clear();
            foreach (var item in results)
            {
                Reports.Add(new BirdStrikeReport
                {
                    ReporterId = item.ReporterId,
                    ReportDate = item.ReportDate,
                    ReportTime = item.ReportTime,
                    Notes = item.ReporterNotes ?? string.Empty
                });
            }
            StatusMessage = $"{Reports.Count} report(s)";
        }
        catch (Exception ex)
        {
            Reports.Clear();
            StatusMessage = "Reports could not be loaded.";
            await DisplayAlertAsync("Unable to load bird strike reports", ex.Message, "OK");
        }
        OnPropertyChanged(nameof(StatusMessage));
    }
}

public sealed class BirdStrikeReport
{
    public Guid ReporterId { get; init; }
    public DateTime ReportDate { get; init; }
    public TimeSpan ReportTime { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string Date => ReportDate.ToString("yyyy-MM-dd");
    public string Time => ReportTime.ToString(@"hh\:mm\:ss");
}
