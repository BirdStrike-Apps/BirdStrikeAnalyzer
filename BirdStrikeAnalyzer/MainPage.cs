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
    private readonly Label _connectionStatus = new()
    {
        Text = "Connecting to Supabase when the app opens…",
        TextColor = Muted,
        FontSize = 12,
        VerticalOptions = LayoutOptions.Center
    };

    private readonly List<AnalyzerReport> _pendingReports = new();
    private readonly VerticalStackLayout _todoList = new() { Spacing = 10 };
    private readonly Label _todoCount = new() { FontSize = 13, TextColor = Color.FromArgb("#64748B") };

    private bool _isLoading;

    public MainPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;
        Title = "Home";
        BackgroundColor = PageBackground;

        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            Padding = new Thickness(20, 14),
            BackgroundColor = PanelBlue
        };
        header.Add(new Label { Text = "Hello, Misty!", FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalOptions = LayoutOptions.Center });
        header.Add(_connectionStatus, 1);

        _homeMap = new GeoapifyMapView
        {
            MinimumHeightRequest = 300,
            BackgroundColor = Colors.White
        };
        var mapTap = new TapGestureRecognizer();
        mapTap.Tapped += async (_, _) => await Shell.Current.GoToAsync("//mapview");
        _homeMap.GestureRecognizers.Add(mapTap);
        var chart = new GraphicsView
        {
            Drawable = new ReportsBarChartDrawable(Array.Empty<string>(), Array.Empty<MonthlySeries>()),
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
        reporters.Add(new Label { Text = "Reporter summaries will appear when connected data is available.", TextColor = Muted, FontSize = 13 });

        var birds = new Label
        {
            Text = "Live building totals are shown on the map.",
            TextColor = Ink,
            Padding = new Thickness(12, 8)
        };

        var middleColumn = new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Panel(Heading("Reporters"), reporters),
                Panel(Heading("Confirmed strikes"), birds)
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoading) return;
        _isLoading = true;
        _connectionStatus.Text = "Connecting to Supabase…";
        try
        {
            await _businessLogic.InitializeAsync();
            var hotspotsTask = _businessLogic.GetBuildingHotspotsAsync();
            var reportsTask = _businessLogic.GetAnalyzerReportsAsync();
            await Task.WhenAll(hotspotsTask, reportsTask);

            var hotspots = await hotspotsTask;
            _homeMap.Show(hotspots);
            _pendingReports.Clear();
            _pendingReports.AddRange((await reportsTask).Where(report => !report.IsConfirmed));
            RenderTodoList();
            _connectionStatus.Text = $"Connected to Supabase · {hotspots.Count} buildings · {_pendingReports.Count} reports to review";
        }
        catch (Exception ex)
        {
            _homeMap.Show(Array.Empty<BuildingReportCount>());
            _pendingReports.Clear();
            RenderTodoList();
            _connectionStatus.Text = $"Supabase connection failed: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
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
            await _businessLogic.ConfirmReportAsync(report.Id);
            report.IsConfirmed = true;
            _pendingReports.Remove(report);
            RenderTodoList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Unable to confirm report", ex.Message, "OK");
        }
    }

}
