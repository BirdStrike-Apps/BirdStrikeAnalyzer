using Microsoft.Maui.Graphics;

namespace BirdStrikeAnalyzer;

/// <summary>
/// Desktop analyzer dashboard. Report loading and confirmation go through an interface,
/// allowing the in-memory sample source to be replaced by the business/database layers.
/// </summary>
public sealed class AnalyzerPage : ContentPage
{
    private readonly IAnalyzerDataSource _dataSource;
    private readonly List<AnalyzerReport> _reports = new();
    private readonly List<AnalyzerReport> _pendingReports = new();
    private readonly Picker _chartTypePicker = new() { Title = "Chart type", WidthRequest = 150 };
    private readonly Picker _groupPicker = new() { Title = "Group by", WidthRequest = 170 };
    private readonly AnalyzerChartDrawable _chart = new();
    private readonly GraphicsView _chartView;
    private readonly Grid _tableGrid = new() { ColumnSpacing = 0, RowSpacing = 0 };
    private readonly VerticalStackLayout _todoList = new() { Spacing = 10 };
    private readonly Label _todoCount = new() { FontSize = 13, TextColor = Color.FromArgb("#64748B") };

    private static readonly IReadOnlyList<AnalyzerColumn> AvailableColumns = new List<AnalyzerColumn>
    {
        new("Reporter", report => report.Reporter, report => report.Reporter),
        new("Building", report => report.Building, report => report.Building),
        new("Door", report => report.Door, report => report.Door),
        new("Date", report => report.ReportedAt.ToLocalTime().ToString("yyyy-MM-dd"), report => report.ReportedAt.ToLocalTime().ToString("yyyyMMdd")),
        new("Time", report => report.ReportedAt.ToLocalTime().ToString("h:mm tt"), report => report.ReportedAt.ToLocalTime().ToString("HHmm")),
        new("Status", report => report.IsConfirmed ? "Confirmed" : "Needs review", report => report.IsConfirmed ? "1" : "0")
    };

    private readonly List<ColumnSelection> _selectedColumns = new();
    private ColumnSelection? _sortColumn;
    private bool _sortAscending = true;

    public AnalyzerPage(IAnalyzerDataSource dataSource)
    {
        _dataSource = dataSource;
        Title = "Analyzer";
        BackgroundColor = Color.FromArgb("#F3F7FB");

        _chartView = new GraphicsView
        {
            Drawable = _chart,
            HeightRequest = 280,
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill
        };

        foreach (var type in new[] { "Bar", "Line", "Scatter" })
            _chartTypePicker.Items.Add(type);
        foreach (var grouping in new[] { "Month", "Building", "Time of day" })
            _groupPicker.Items.Add(grouping);
        _chartTypePicker.SelectedIndex = 0;
        _groupPicker.SelectedIndex = 0;
        _chartTypePicker.SelectedIndexChanged += (_, _) => RenderChart();
        _groupPicker.SelectedIndexChanged += (_, _) => RenderChart();

        _selectedColumns.AddRange(new[]
        {
            new ColumnSelection(AvailableColumns[0]),
            new ColumnSelection(AvailableColumns[1]),
            new ColumnSelection(AvailableColumns[2]),
            new ColumnSelection(AvailableColumns[3])
        });

        Content = BuildDesktopLayout();
        Loaded += async (_, _) => await LoadAsync();
    }

    private View BuildDesktopLayout()
    {
        var root = new Grid
        {
            Padding = new Thickness(22, 18),
            RowSpacing = 16,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 18
        };
        header.Children.Add(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                UiStyle.Heading("Analyzer", 30),
                new Label { Text = "Birdstrike reports and trends", FontSize = 14, TextColor = Color.FromArgb("#475467") }
            }
        });
        var exportButton = new Button
        {
            Text = "Export",
            FontSize = 14,
            Padding = new Thickness(18, 8),
            VerticalOptions = LayoutOptions.Center
        };
        exportButton.Clicked += async (_, _) => await DisplayAlert("Export", "Export can be connected to the report data source later.", "OK");
        Grid.SetColumn(exportButton, 1);
        header.Children.Add(exportButton);
        root.Children.Add(header);

        var content = new Grid
        {
            ColumnSpacing = 18,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1.85, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        var leftColumn = new Grid
        {
            RowSpacing = 16,
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(0.95, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1.05, GridUnitType.Star) }
            }
        };
        leftColumn.Children.Add(BuildGraphCard());
        var tableCard = BuildTableCard();
        Grid.SetRow(tableCard, 1);
        leftColumn.Children.Add(tableCard);
        content.Children.Add(leftColumn);

        var todoCard = BuildTodoCard();
        Grid.SetColumn(todoCard, 1);
        content.Children.Add(todoCard);
        Grid.SetRow(content, 1);
        root.Children.Add(content);
        return root;
    }

    private View BuildGraphCard()
    {
        var controls = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children = { new Label { Text = "Chart type", FontSize = 12, TextColor = Color.FromArgb("#64748B") }, _chartTypePicker }
                },
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children = { new Label { Text = "Group by", FontSize = 12, TextColor = Color.FromArgb("#64748B") }, _groupPicker }
                }
            }
        };

        return UiStyle.Card(new Grid
        {
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                new Label { Text = "Graphs", FontSize = 21, FontAttributes = FontAttributes.Bold },
                Place(controls, 0, 1),
                Place(_chartView, 0, 2)
            }
        });
    }

    private View BuildTableCard()
    {
        var addColumnButton = new Button
        {
            Text = "+ Add column",
            FontSize = 13,
            Padding = new Thickness(12, 6),
            HorizontalOptions = LayoutOptions.Start
        };
        addColumnButton.Clicked += async (_, _) => await AddColumnAsync();

        var tableScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Both,
            Content = _tableGrid
        };
        var content = new Grid
        {
            RowSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                new Label { Text = "Reports table", FontSize = 21, FontAttributes = FontAttributes.Bold },
                Place(addColumnButton, 0, 1),
                Place(tableScroll, 0, 2)
            }
        };
        return UiStyle.Card(content);
    }

    private View BuildTodoCard()
    {
        var todoScroll = new ScrollView
        {
            VerticalOptions = LayoutOptions.Fill,
            Content = _todoList
        };
        var content = new Grid
        {
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                new Label { Text = "To-Do", FontSize = 23, FontAttributes = FontAttributes.Bold },
                Place(new Label { Text = "Reports awaiting your confirmation", FontSize = 13, TextColor = Color.FromArgb("#64748B") }, 0, 1),
                Place(_todoCount, 0, 2),
                Place(todoScroll, 0, 3)
            }
        };
        return UiStyle.Card(content);
    }

    private static T Place<T>(T view, int column, int row) where T : View
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        return view;
    }

    private async Task LoadAsync()
    {
        try
        {
            var reports = await _dataSource.GetReportsAsync();
            _reports.Clear();
            _reports.AddRange(reports);
            _pendingReports.Clear();
            _pendingReports.AddRange(_reports.Where(report => !report.IsConfirmed));
            RenderTable();
            RenderChart();
            RenderTodoList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Unable to load reports", ex.Message, "OK");
        }
    }

    private async Task AddColumnAsync()
    {
        var nextColumn = AvailableColumns.FirstOrDefault(candidate =>
            _selectedColumns.All(selected => selected.Column.Name != candidate.Name));
        if (nextColumn is null)
        {
            await DisplayAlert("All columns shown", "Every available report column is already in the table.", "OK");
            return;
        }

        _selectedColumns.Add(new ColumnSelection(nextColumn));
        RenderTable();
    }

    private void RemoveColumn(ColumnSelection selection)
    {
        if (_selectedColumns.Count <= 1)
            return;

        _selectedColumns.Remove(selection);
        if (_sortColumn == selection)
            _sortColumn = null;
        RenderTable();
    }

    private void RenderTable()
    {
        _tableGrid.Children.Clear();
        _tableGrid.ColumnDefinitions.Clear();
        _tableGrid.RowDefinitions.Clear();

        for (var columnIndex = 0; columnIndex < _selectedColumns.Count; columnIndex++)
            _tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
        _tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var columnIndex = 0; columnIndex < _selectedColumns.Count; columnIndex++)
        {
            var selection = _selectedColumns[columnIndex];
            var picker = new Picker
            {
                Title = "Select column",
                FontSize = 12,
                WidthRequest = 135,
                HorizontalOptions = LayoutOptions.Fill
            };
            foreach (var column in AvailableColumns)
                picker.Items.Add(column.Name);
            picker.SelectedIndex = AvailableColumns.ToList().FindIndex(column => column.Name == selection.Column.Name);
            picker.SelectedIndexChanged += (_, _) =>
            {
                if (picker.SelectedIndex < 0)
                    return;
                selection.Column = AvailableColumns[picker.SelectedIndex];
                _sortColumn = selection;
                _sortAscending = true;
                RenderTable();
            };

            var sortButton = new Button
            {
                Text = _sortColumn == selection ? (_sortAscending ? "↑ Ascending" : "↓ Descending") : "↕ Sort",
                FontSize = 11,
                Padding = new Thickness(8, 5),
                MinimumHeightRequest = 34
            };
            sortButton.Clicked += (_, _) =>
            {
                if (_sortColumn == selection)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = selection;
                    _sortAscending = true;
                }
                RenderTable();
            };

            var removeButton = new Button
            {
                Text = "×",
                FontSize = 14,
                Padding = new Thickness(6, 4),
                MinimumWidthRequest = 32,
                MinimumHeightRequest = 34,
                IsEnabled = _selectedColumns.Count > 1
            };
            removeButton.Clicked += (_, _) => RemoveColumn(selection);

            var header = new VerticalStackLayout
            {
                Spacing = 5,
                Padding = new Thickness(8),
                BackgroundColor = Color.FromArgb("#DCEBFA"),
                Children =
                {
                    new HorizontalStackLayout { Spacing = 3, Children = { picker, removeButton } },
                    sortButton
                }
            };
            _tableGrid.Children.Add(Place(header, columnIndex, 0));
        }

        var dataRows = GetSortedReports().ToList();
        if (dataRows.Count == 0)
        {
            _tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _tableGrid.Children.Add(Place(new Label { Text = "No reports available.", Padding = new Thickness(10), TextColor = Color.FromArgb("#64748B") }, 0, 1));
            return;
        }

        for (var rowIndex = 0; rowIndex < dataRows.Count; rowIndex++)
        {
            var row = rowIndex + 1;
            _tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (var columnIndex = 0; columnIndex < _selectedColumns.Count; columnIndex++)
            {
                var report = dataRows[rowIndex];
                var column = _selectedColumns[columnIndex].Column;
                var cell = new Border
                {
                    Stroke = Color.FromArgb("#E1E7EF"),
                    StrokeThickness = 1,
                    BackgroundColor = rowIndex % 2 == 0 ? Colors.White : Color.FromArgb("#F8FAFD"),
                    Padding = new Thickness(10, 9),
                    Content = new Label
                    {
                        Text = column.DisplayValue(report),
                        FontSize = 12,
                        LineBreakMode = LineBreakMode.TailTruncation
                    }
                };
                _tableGrid.Children.Add(Place(cell, columnIndex, row));
            }
        }
    }

    private IEnumerable<AnalyzerReport> GetSortedReports()
    {
        if (_sortColumn is null)
            return _reports;

        var sorted = _reports.OrderBy(report => _sortColumn.Column.SortValue(report), StringComparer.CurrentCultureIgnoreCase);
        return _sortAscending ? sorted : sorted.Reverse();
    }

    private void RenderChart()
    {
        var (labels, values) = GetChartData();
        _chart.ChartType = _chartTypePicker.SelectedItem?.ToString() ?? "Bar";
        _chart.Labels = labels;
        _chart.Values = values;
        _chartView.Invalidate();
    }

    private (IReadOnlyList<string> Labels, IReadOnlyList<int> Values) GetChartData()
    {
        var grouping = _groupPicker.SelectedItem?.ToString() ?? "Month";
        if (grouping == "Building")
        {
            var groups = _reports.GroupBy(report => string.IsNullOrWhiteSpace(report.Building) ? "Unknown" : report.Building)
                .OrderBy(group => group.Key).ToList();
            return groups.Count == 0
                ? (new[] { "No data" }, new[] { 0 })
                : (groups.Select(group => group.Key).ToList(), groups.Select(group => group.Count()).ToList());
        }

        if (grouping == "Time of day")
        {
            var labels = new[] { "12–6a", "6a–12p", "12–6p", "6p–12a" };
            var values = new int[labels.Length];
            foreach (var report in _reports)
                values[report.ReportedAt.ToLocalTime().Hour / 6]++;
            return (labels, values);
        }

        var months = Enumerable.Range(0, 8).Select(offset => DateTime.Today.AddMonths(offset - 7)).ToArray();
        var monthCounts = months.Select(month => _reports.Count(report =>
        {
            var reported = report.ReportedAt.ToLocalTime();
            return reported.Year == month.Year && reported.Month == month.Month;
        })).ToArray();
        return (months.Select(month => month.ToString("MMM")).ToArray(), monthCounts);
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
            RenderTable();
            RenderChart();
            RenderTodoList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Unable to confirm report", ex.Message, "OK");
        }
    }

    private sealed class ColumnSelection(AnalyzerColumn column)
    {
        public AnalyzerColumn Column { get; set; } = column;
    }

    private sealed record AnalyzerColumn(string Name, Func<AnalyzerReport, string> DisplayValue, Func<AnalyzerReport, string> SortValue);
}

/// <summary>Adapter boundary for connecting this page to the app's business/database layers.</summary>
public interface IAnalyzerDataSource
{
    Task<IReadOnlyList<AnalyzerReport>> GetReportsAsync(CancellationToken cancellationToken = default);
    Task ConfirmReportAsync(Guid reportId, CancellationToken cancellationToken = default);
}

/// <summary>In-memory sample source so the page works before a Supabase adapter is added.</summary>
public sealed class SampleAnalyzerDataSource : IAnalyzerDataSource
{
    private readonly List<AnalyzerReport> _reports = new()
    {
        new() { Id = Guid.Parse("a5ecfcbf-758a-4d60-a63f-cf8be36f0844"), Reporter = "James Anderson", Building = "Halsey Science Center", Door = "Door 2", ReportedAt = DateTimeOffset.Now.AddDays(-2).AddHours(-1), IsConfirmed = false },
        new() { Id = Guid.Parse("1c5c3c0b-ae1b-4660-a4fe-ace837633879"), Reporter = "Bob Hawk", Building = "Reeve Memorial Union", Door = "Door 7", ReportedAt = DateTimeOffset.Now.AddDays(-5), IsConfirmed = false },
        new() { Id = Guid.Parse("ab23b65d-fef2-4d13-8b9e-117773759842"), Reporter = "Howard Smith", Building = "Student Recreation and Wellness Center", Door = "Door 1", ReportedAt = DateTimeOffset.Now.AddDays(-12).AddHours(3), IsConfirmed = false },
        new() { Id = Guid.Parse("8dc5bab7-e9d1-46b8-a522-e638db62c66a"), Reporter = "Jen Thomas", Building = "Swart Hall", Door = "Door 4", ReportedAt = DateTimeOffset.Now.AddDays(-20).AddHours(-4), IsConfirmed = true },
        new() { Id = Guid.Parse("8ae55e61-8e06-477a-9a1f-e276144421a3"), Reporter = "Leonard Williams", Building = "Pollock Library", Door = "North Entrance", ReportedAt = DateTimeOffset.Now.AddDays(-35).AddHours(2), IsConfirmed = true }
    };

    public Task<IReadOnlyList<AnalyzerReport>> GetReportsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AnalyzerReport>>(_reports.ToList());

    public Task ConfirmReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = _reports.SingleOrDefault(item => item.Id == reportId)
            ?? throw new KeyNotFoundException("The birdstrike report could not be found.");
        report.IsConfirmed = true;
        return Task.CompletedTask;
    }
}

/// <summary>UI-neutral report DTO mapped from the report model in the data source.</summary>
public sealed class AnalyzerReport
{
    public Guid Id { get; init; }
    public string Reporter { get; init; } = string.Empty;
    public string Building { get; init; } = string.Empty;
    public string Door { get; init; } = string.Empty;
    public DateTimeOffset ReportedAt { get; init; }
    public bool IsConfirmed { get; set; }
}

internal sealed class AnalyzerChartDrawable : IDrawable
{
    public string ChartType { get; set; } = "Bar";
    public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        const float left = 38;
        const float top = 18;
        const float bottom = 42;
        const float right = 18;
        var plot = new RectF(left, top, Math.Max(1, dirtyRect.Width - left - right), Math.Max(1, dirtyRect.Height - top - bottom));
        canvas.StrokeColor = Color.FromArgb("#D8DEE8");
        canvas.StrokeSize = 1;
        canvas.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom);
        canvas.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        if (Values.Count == 0 || Labels.Count == 0)
        {
            canvas.FontColor = Color.FromArgb("#64748B");
            canvas.DrawString("No chart data", plot, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
            return;
        }

        var maximum = Math.Max(1, Values.Max());
        var slotWidth = plot.Width / Values.Count;
        var points = new List<PointF>();
        for (var index = 0; index < Values.Count; index++)
        {
            var x = plot.Left + slotWidth * (index + 0.5f);
            var y = plot.Bottom - (Values[index] / (float)maximum) * (plot.Height - 24) - 8;
            points.Add(new PointF(x, y));
        }

        if (ChartType == "Bar")
        {
            for (var index = 0; index < Values.Count; index++)
            {
                var barWidth = Math.Min(38, slotWidth * 0.58f);
                var height = Math.Max(2, plot.Bottom - points[index].Y);
                canvas.FillColor = index % 2 == 0 ? Color.FromArgb("#1682D4") : Color.FromArgb("#6B8DE3");
                canvas.FillRectangle(points[index].X - barWidth / 2, points[index].Y, barWidth, height);
            }
        }
        else
        {
            if (ChartType == "Line" && points.Count > 1)
            {
                canvas.StrokeColor = Color.FromArgb("#1682D4");
                canvas.StrokeSize = 3;
                for (var index = 1; index < points.Count; index++)
                    canvas.DrawLine(points[index - 1].X, points[index - 1].Y, points[index].X, points[index].Y);
            }

            canvas.FillColor = Color.FromArgb("#1682D4");
            foreach (var point in points)
                canvas.FillCircle(point.X, point.Y, 10);
        }

        canvas.FontColor = Color.FromArgb("#475467");
        canvas.FontSize = 10;
        for (var index = 0; index < Labels.Count; index++)
        {
            var labelRect = new RectF(plot.Left + slotWidth * index, plot.Bottom + 7, slotWidth, 24);
            canvas.DrawString(Labels[index], labelRect, HorizontalAlignment.Center, VerticalAlignment.Top);
        }
        canvas.RestoreState();
    }
}
