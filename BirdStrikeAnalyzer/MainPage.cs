using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

public sealed class MainPage : ContentPage
{
    private static readonly Color PageBackground = Color.FromArgb("#F3F7FB");
    private static readonly Color PanelBlue = Color.FromArgb("#9CCCF8");
    private static readonly Color AccentBlue = Color.FromArgb("#1682D4");
    private static readonly Color Ink = Color.FromArgb("#101820");
    private static readonly Color Muted = Color.FromArgb("#64748B");

    private readonly BusinessLogic _businessLogic;
    private readonly GeoapifyMapView _homeMap;
    private readonly Label _connectionStatus = new() { Text = "Connecting to Supabase…", TextColor = Muted, FontSize = 12, VerticalOptions = LayoutOptions.Center };
    private readonly VerticalStackLayout _todoList = new() { Spacing = 10 };
    private readonly Label _todoCount = new() { FontSize = 13, TextColor = Muted };
    private readonly VerticalStackLayout _reportersTable = new() { Spacing = 0 };
    private readonly VerticalStackLayout _buildingsTable = new() { Spacing = 0 };
    private readonly GraphicsView _monthlyChart;
    private bool _loading;

    public MainPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;
        Title = "Home";
        BackgroundColor = PageBackground;

        var connect = new Button { Text = "Refresh data", BackgroundColor = AccentBlue, TextColor = Colors.White, CornerRadius = 4, Padding = new Thickness(14, 6), MinimumHeightRequest = 36 };
        connect.Clicked += async (_, _) => await LoadDashboardAsync();
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12, Padding = new Thickness(20, 14), BackgroundColor = PanelBlue };
        header.Add(new Label { Text = "BirdStrike Analyzer", FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalOptions = LayoutOptions.Center });
        header.Add(new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center, Children = { _connectionStatus, connect } }, 1);

        _homeMap = new GeoapifyMapView { MinimumHeightRequest = 300, BackgroundColor = Colors.White };
        var mapTap = new TapGestureRecognizer();
        mapTap.Tapped += async (_, _) => await Shell.Current.GoToAsync("//mapview");
        _homeMap.GestureRecognizers.Add(mapTap);

        _monthlyChart = new GraphicsView { Drawable = new ReportsBarChartDrawable(Array.Empty<string>(), Array.Empty<MonthlySeries>()), HeightRequest = 210, BackgroundColor = Colors.White };
        var leftColumn = new Grid { RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }, RowSpacing = 16 };
        leftColumn.Add(Panel(Heading("Confirmed strikes by building"), _homeMap));
        leftColumn.Add(Panel(Heading("Confirmed strikes per month"), _monthlyChart), 0, 1);

        // Give the reporter directory more room while keeping its rows inside
        // the card independently scrollable.
        var reportersList = new ScrollView
        {
            Content = _reportersTable,
            VerticalOptions = LayoutOptions.Fill,
            HeightRequest = 400
        };
        var reporterPanel = Panel(Heading("Reporters"), reportersList);
        reporterPanel.HeightRequest = 460;
        reporterPanel.VerticalOptions = LayoutOptions.Start;
        // Keep this dashboard card bounded so a large building list does not
        // increase the page height; only the rows inside the card scroll.
        var confirmedList = new ScrollView
        {
            Content = _buildingsTable,
            VerticalOptions = LayoutOptions.Fill,
            HeightRequest = 220
        };
        var confirmedPanel = Panel(Heading("Confirmed Strikes — past 30 days"), confirmedList);
        confirmedPanel.HeightRequest = 280;
        confirmedPanel.VerticalOptions = LayoutOptions.Start;
        var middleColumn = new VerticalStackLayout { Spacing = 16, Children = { reporterPanel, confirmedPanel } };

        var todoContent = new Grid { RowSpacing = 10, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        todoContent.Add(new Label { Text = "To-Do", FontSize = 23, FontAttributes = FontAttributes.Bold, TextColor = Ink });
        todoContent.Add(new Label { Text = "Submissions awaiting confirmation", FontSize = 13, TextColor = Muted }, 0, 1);
        todoContent.Add(_todoCount, 0, 2);
        todoContent.Add(new ScrollView { VerticalOptions = LayoutOptions.Fill, Content = _todoList }, 0, 3);
        var todoColumn = UiStyle.Card(todoContent);

        var body = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(3, GridUnitType.Star)), new ColumnDefinition(new GridLength(300)), new ColumnDefinition(new GridLength(300)) },
            ColumnSpacing = 16,
            Padding = new Thickness(20)
        };
        body.Add(leftColumn);
        body.Add(middleColumn, 1);
        body.Add(todoColumn, 2);

        var pageGrid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Children = { header } };
        pageGrid.Add(new ScrollView { Content = body }, 0, 1);
        Content = pageGrid;
        Loaded += async (_, _) => await LoadDashboardAsync();
    }

    private static Label Heading(string text) => new() { Text = text, FontSize = 20, TextColor = Ink };

    private static Border Panel(View heading, View content) => new()
    {
        BackgroundColor = PanelBlue,
        Stroke = Colors.Transparent,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
        Padding = new Thickness(16),
        Content = new VerticalStackLayout { Spacing = 10, Children = { heading, content } }
    };

    private async Task LoadDashboardAsync()
    {
        if (_loading) return;
        _loading = true;
        _connectionStatus.Text = "Loading Supabase data…";
        try
        {
            var pendingTask = _businessLogic.GetPendingReportsAsync();
            var reportersTask = _businessLogic.GetReporterAssignmentsAsync();
            var buildingsTask = _businessLogic.GetBuildingConfirmationsLast30DaysAsync();
            var monthlyTask = _businessLogic.GetMonthlyConfirmationsAsync();
            var mapTask = _businessLogic.GetBuildingHotspotsAsync();
            await Task.WhenAll(pendingTask, reportersTask, buildingsTask, monthlyTask, mapTask);

            RenderTodoList(await pendingTask);
            RenderReporters(await reportersTask);
            RenderBuildingCounts(await buildingsTask);
            var monthly = await monthlyTask;
            _monthlyChart.Drawable = new ReportsBarChartDrawable(monthly.Months,
                new[] { new MonthlySeries("Confirmed strikes", "#1682D4", monthly.Counts.ToArray()) });
            _monthlyChart.Invalidate();
            _homeMap.Show(await mapTask);
            _connectionStatus.Text = "Supabase data loaded.";
        }
        catch (Exception ex)
        {
            _connectionStatus.Text = "Supabase load failed.";
            await DisplayAlertAsync("Unable to load dashboard", ex.Message, "OK");
        }
        finally { _loading = false; }
    }

    private void RenderReporters(IReadOnlyList<ReporterBuildingAssignment> reporters)
    {
        _reportersTable.Children.Clear();
        AddTableHeader(_reportersTable, "Reporter", "Assigned building");
        if (reporters.Count == 0) _reportersTable.Children.Add(EmptyRow("No reporters found."));
        foreach (var reporter in reporters) AddTableRow(_reportersTable, reporter.Reporter, reporter.Building);
    }

    private void RenderBuildingCounts(IReadOnlyList<BuildingConfirmationCount> buildings)
    {
        _buildingsTable.Children.Clear();
        AddTableHeader(_buildingsTable, "Building", "Confirmed");
        if (buildings.Count == 0) _buildingsTable.Children.Add(EmptyRow("No buildings found."));
        foreach (var building in buildings) AddTableRow(_buildingsTable, building.Building, building.Count.ToString());
    }

    private static void AddTableHeader(VerticalStackLayout table, string first, string second)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(1, GridUnitType.Auto)) }, Padding = new Thickness(8, 9), BackgroundColor = Color.FromArgb("#EAF3FC") };
        grid.Add(new Label { Text = first, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Ink });
        grid.Add(new Label { Text = second, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Ink, HorizontalTextAlignment = TextAlignment.End }, 1);
        table.Children.Add(grid);
    }

    private static void AddTableRow(VerticalStackLayout table, string first, string second)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(1, GridUnitType.Auto)) }, Padding = new Thickness(8, 9), ColumnSpacing = 8 };
        grid.Add(new Label { Text = first, FontSize = 13, TextColor = Ink, VerticalTextAlignment = TextAlignment.Center });
        grid.Add(new Label { Text = second, FontSize = 13, TextColor = Ink, HorizontalTextAlignment = TextAlignment.End, VerticalTextAlignment = TextAlignment.Center });
        table.Children.Add(new Border { Stroke = Color.FromArgb("#E2E8F0"), StrokeThickness = 0.5, Content = grid });
    }

    private static View EmptyRow(string text) => new Label { Text = text, TextColor = Muted, FontSize = 13, Padding = new Thickness(8) };

    private void RenderTodoList(IReadOnlyList<AnalyzerReport> reports)
    {
        _todoList.Children.Clear();
        _todoCount.Text = $"{reports.Count} submission(s) need review";
        if (reports.Count == 0)
        {
            _todoList.Children.Add(new Label { Text = "All submissions are confirmed.", TextColor = Muted });
            return;
        }

        foreach (var report in reports)
        {
            var confirmButton = new Button
            {
                Text = "Confirm bird strike",
                FontSize = 13,
                Padding = new Thickness(12, 7),
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = AccentBlue,
                TextColor = Colors.White,
                CornerRadius = 5
            };
            confirmButton.Clicked += async (_, _) => await ConfirmAsync(report.Id);

            var details = new VerticalStackLayout { Spacing = 5, Children =
            {
                new Label { Text = report.Reporter, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Ink },
                new Label { Text = report.ReportDateText, FontSize = 13, TextColor = Ink },
                new Label { Text = report.ReportTimeText, FontSize = 13, TextColor = Ink },
                new Label { Text = $"{report.Building} — {report.Door}", FontSize = 12, TextColor = Muted },
                confirmButton
            }};
            _todoList.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#D5E2EF"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = 12,
                Content = details
            });
        }
    }

    private async Task ConfirmAsync(Guid submissionId)
    {
        try
        {
            await _businessLogic.ConfirmReportAsync(submissionId);
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Unable to confirm report", ex.Message, "OK");
        }
    }
}
