using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace BirdStrikeAnalyzer;

public partial class BirdStrikesPage : ContentPage
{
    public ObservableCollection<BirdStrikeReport> Reports { get; } = new();
    public bool IsSummaryVisible { get; private set; } = true;

    public BirdStrikesPage()
    {
        InitializeComponent();
        BindingContext = this;
        ConfirmedFilter.SelectedIndex = 0;
    }



    private void OnMenuClicked(object? sender, EventArgs e)
    {
        IsSummaryVisible = !IsSummaryVisible;
        OnPropertyChanged(nameof(IsSummaryVisible));
    }

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        FilterPanel.IsVisible = !FilterPanel.IsVisible;
        FilterButton.Text = FilterPanel.IsVisible ? "Close filters" : "Filter";
    }

    private void OnClearFiltersClicked(object? sender, EventArgs e)
    {
        StartDate.Date = DateTime.Today;
        EndDate.Date = DateTime.Today;
        StartTime.Time = TimeSpan.Zero;
        EndTime.Time = new TimeSpan(23, 59, 0);
        ReporterFilter.Text = string.Empty;
        LocationFilter.Text = string.Empty;
        ConfirmedFilter.SelectedIndex = 0;
    }

    private void OnApplyFiltersClicked(object? sender, EventArgs e)
    {
        // Filter values are ready to be connected to the report data source.
        FilterPanel.IsVisible = false;
        FilterButton.Text = "Filter";
    }
}

public sealed class BirdStrikeReport
{
    public string Reporter { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
}
