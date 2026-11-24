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
    private readonly GraphicsView _homeMap;
    private readonly Label _connectionStatus = new()
    {
        Text = "Supabase has not been contacted.",
        TextColor = Muted,
        FontSize = 12,
        VerticalOptions = LayoutOptions.Center
    };

    private readonly IAnalyzerDataSource _dataSource;
    private readonly List<AnalyzerReport> _pendingReports = new();
    private readonly VerticalStackLayout _todoList = new() { Spacing = 10 };
    private readonly Label _todoCount = new() { FontSize = 13, TextColor = Color.FromArgb("#64748B") };

    public MainPage(BusinessLogic businessLogic, IAnalyzerDataSource dataSource)
    {
        _businessLogic = businessLogic;
        _dataSource = dataSource;
        Title = "Home";
        BackgroundColor = PageBackground;

        var connect = new Button
        {
            Text = "Connect to Supabase",
            BackgroundColor = AccentBlue,
            TextColor = Colors.White,
            CornerRadius = 4,
            Padding = new Thickness(14, 6),
            MinimumHeightRequest = 36
        };
        connect.Clicked += async (_, _) => await ConnectAsync();

        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            Padding = new Thickness(20, 14),
            BackgroundColor = PanelBlue
        };
        header.Add(new Label { Text = "Hello, Misty!", FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalOptions = LayoutOptions.Center });
        var right = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center, Children = { _connectionStatus, connect } };
        header.Add(right, 1);

        _homeMap = new GraphicsView
        {
            Drawable = new CampusMapDrawable(HomeDashboardData.Buildings),
            MinimumHeightRequest = 300,
            BackgroundColor = Colors.White
        };
        var mapTap = new TapGestureRecognizer();
        mapTap.Tapped += async (_, _) => await Shell.Current.GoToAsync("//mapview");
        _homeMap.GestureRecognizers.Add(mapTap);
        var chart = new GraphicsView
        {
            Drawable = new ReportsBarChartDrawable(HomeDashboardData.Months, HomeDashboardData.Series),
            HeightRequest = 190,
            BackgroundColor = Colors.White
        };

        var mapPanel = Panel(Heading("Bird reports by building"), _homeMap);
        var chartPanel = Panel(Heading("Reports per month"), chart);

        var leftColumn = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            RowSpacing = 16
        };
        leftColumn.Add(mapPanel);
        leftColumn.Add(chartPanel, 0, 1);

        var reporters = new VerticalStackLayout { Spacing = 10 };
        foreach (var r in HomeDashboardData.Reporters)
            reporters.Add(Card(new Label { Text = r.Name, FontAttributes = FontAttributes.Bold, TextColor = Ink },
                               new Label { Text = $"Buildings: {r.Buildings}", TextColor = Ink }));

        var birds = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, RowSpacing = 4, Padding = new Thickness(12, 8), BackgroundColor = Colors.White };
        var row = 0;
        foreach (var (building, count) in HomeDashboardData.BirdsPerMonth)
        {
            birds.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            birds.Add(new Label { Text = building, TextColor = Ink }, 0, row);
            birds.Add(new Label { Text = count.ToString(), FontAttributes = FontAttributes.Bold, TextColor = Ink }, 1, row);
            row++;
        }

        var middleColumn = new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Panel(Heading("Reporters"), reporters),
                Panel(Heading("Birds per month"), birds)
            }
        };

        // To-Do card: same structure as AnalyzerPage (title, subtitle, count, scrolling list of reports to confirm).
        var todoScroll = new ScrollView { VerticalOptions = LayoutOptions.Fill, Content = _todoList };
        var todoContent = new Grid
        {
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };
        todoContent.Add(new Label { Text = "To-Do", FontSize = 23, FontAttributes = FontAttributes.Bold });
        todoContent.Add(new Label { Text = "Reports awaiting your confirmation", FontSize = 13, TextColor = Color.FromArgb("#64748B") }, 0, 1);
        todoContent.Add(_todoCount, 0, 2);
        todoContent.Add(todoScroll, 0, 3);
        var todoColumn = UiStyle.Card(todoContent);
        Loaded += async (_, _) => await LoadTodoAsync();

        // Wide layout like the mockup: map + reporters side by side, To-Do on the right.
        var body = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(3, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(300)),
                new ColumnDefinition(new GridLength(300))
            },
            ColumnSpacing = 16,
            Padding = new Thickness(20)
        };
        body.Add(leftColumn);
        body.Add(middleColumn, 1);
        body.Add(todoColumn, 2);

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children = { header }
        };
        var scroll = new ScrollView { Content = body };
        ((Grid)Content).Add(scroll, 0, 1);
    }

    private static Label Heading(string text) =>
        new() { Text = text, FontSize = 20, TextColor = Ink };

    private static Border Card(params View[] content)
    {
        var stack = new VerticalStackLayout { Padding = new Thickness(12, 8) };
        foreach (var v in content) stack.Add(v);
        return new Border { BackgroundColor = Colors.White, Stroke = Colors.Transparent, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }, Content = stack };
    }

    private static Border Panel(View heading, View content) => new()
    {
        BackgroundColor = PanelBlue,
        Stroke = Colors.Transparent,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
        Padding = new Thickness(16),
        Content = new VerticalStackLayout { Spacing = 10, Children = { heading, content } }
    };

    private async Task LoadTodoAsync()
    {
        try
        {
            var reports = await _dataSource.GetReportsAsync();
            _pendingReports.Clear();
            _pendingReports.AddRange(reports.Where(report => !report.IsConfirmed));
            RenderTodoList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Unable to load reports", ex.Message, "OK");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_businessLogic.IsAuthenticated)
        {
            _homeMap.Drawable = new CampusMapDrawable(HomeDashboardData.Buildings);
            _homeMap.Invalidate();
            return;
        }

        try
        {
            var buildings = await _businessLogic.GetBuildingHotspotsAsync();
            _homeMap.Drawable = new CampusMapDrawable(buildings);
            _homeMap.Invalidate();
        }
        catch
        {
            // Keep the existing sample map available when Supabase is unavailable.
            _homeMap.Drawable = new CampusMapDrawable(HomeDashboardData.Buildings);
            _homeMap.Invalidate();
        }
    }

    private void RenderTodoList()
    {
        _todoList.Children.Clear();
        _todoCount.Text = $"{_pendingReports.Count} report(s) need review";
        if (_pendingReports.Count == 0)
        {
            _todoList.Children.Add(new Label { Text = "All reports are confirmed.", TextColor = Color.FromArgb("#64748B") });
            return;
        }

        foreach (var report in _pendingReports.ToList())
        {
            var confirmButton = new Button
            {
                Text = "Confirm report",
                FontSize = 13,
                Padding = new Thickness(12, 7),
                HorizontalOptions = LayoutOptions.End
            };
            confirmButton.Clicked += async (_, _) => await ConfirmAsync(report);

            _todoList.Children.Add(UiStyle.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = $"{report.Building} — {report.Door}", FontSize = 15, FontAttributes = FontAttributes.Bold },
                    new Label { Text = report.Reporter, FontSize = 13 },
                    new Label { Text = report.ReportedAt.ToLocalTime().ToString("MMM d, yyyy  h:mm tt"), FontSize = 12, TextColor = Color.FromArgb("#64748B") },
                    confirmButton
                }
            }));
        }
    }

    private async Task ConfirmAsync(AnalyzerReport report)
    {
        try
        {
            await _dataSource.ConfirmReportAsync(report.Id);
            report.IsConfirmed = true;
            _pendingReports.Remove(report);
            RenderTodoList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Unable to confirm report", ex.Message, "OK");
        }
    }

    private async Task ConnectAsync()
    {
        _connectionStatus.Text = "Connecting…";
        try
        {
            var rows = await _businessLogic.GetSampleItemsAsync();
            _connectionStatus.Text = $"Connected. Read {rows.Count} sample row(s).";
        }
        catch (Exception ex)
        {
            _connectionStatus.Text = ex.Message;
        }
    }
}

