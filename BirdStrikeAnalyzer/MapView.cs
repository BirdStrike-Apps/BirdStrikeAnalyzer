using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

/// <summary>Expanded campus map view using the same hotspot drawable and sample data as Home.</summary>
public sealed class MapViewPage : ContentPage
{
    private static readonly Color PanelBlue = Color.FromArgb("#9CCCF8");
    private static readonly Color Ink = Color.FromArgb("#101820");
    private static readonly Color Muted = Color.FromArgb("#64748B");
    private readonly BusinessLogic _businessLogic;
    private readonly GeoapifyMapView _map;
    private readonly VerticalStackLayout _hotspotList = new() { Spacing = 8 };
    private readonly Label _status = new() { FontSize = 12, TextColor = Muted };

    public MapViewPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;
        Title = "Map View";
        BackgroundColor = Color.FromArgb("#F3F7FB");

        _map = new GeoapifyMapView
        {
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var mapPanelContent = new Grid
        {
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                new Label { Text = "Campus birdstrike map", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Ink },
                Place(_map, 0, 1)
            }
        };
        var mapPanel = new Border
        {
            BackgroundColor = PanelBlue,
            Stroke = Colors.Transparent,
            Padding = new Thickness(16),
            Content = mapPanelContent
        };

        RenderHotspots(Array.Empty<BuildingReportCount>());

        var detailsPanel = UiStyle.Card(new Grid
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
                new Label { Text = "Hotspots", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Ink },
                Place(new Label { Text = "Larger, warmer circles indicate more reports.", FontSize = 12, TextColor = Muted }, 0, 1),
                Place(new ScrollView { Content = _hotspotList }, 0, 2)
            }
        });

        var body = new Grid
        {
            ColumnSpacing = 16,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(280) }
            },
            Children = { mapPanel }
        };
        Grid.SetColumn(detailsPanel, 1);
        body.Children.Add(detailsPanel);

        var refreshButton = new Button { Text = "Refresh", FontSize = 13, Padding = new Thickness(14, 7), VerticalOptions = LayoutOptions.Center };
        refreshButton.Clicked += async (_, _) => await LoadHotspotsAsync();
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        UiStyle.Heading("Map View", 30),
                        new Label { Text = "Explore birdstrike report hotspots across campus.", FontSize = 14, TextColor = Muted },
                        _status
                    }
                }
            }
        };
        Grid.SetColumn(refreshButton, 1);
        header.Children.Add(refreshButton);

        Content = new Grid
        {
            Padding = new Thickness(20, 16),
            RowSpacing = 16,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                header,
                Place(body, 0, 1)
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHotspotsAsync();
    }

    private async Task LoadHotspotsAsync()
    {
        _status.Text = "Loading hotspots from Supabase…";
        try
        {
            var hotspots = await _businessLogic.GetBuildingHotspotsAsync();
            if (hotspots.Count == 0)
            {
                _status.Text = "No building and door locations were returned.";
                RenderHotspots(Array.Empty<BuildingReportCount>());
                return;
            }
            RenderHotspots(hotspots);
            _status.Text = $"{hotspots.Count} campus building(s) loaded from Supabase";
        }
        catch (Exception ex)
        {
            RenderHotspots(Array.Empty<BuildingReportCount>());
            _status.Text = $"Unable to load Supabase hotspots: {ex.Message}";
        }
    }

    private void RenderHotspots(IReadOnlyList<BuildingReportCount> buildings)
    {
        _map.Show(buildings);
        _hotspotList.Children.Clear();
        if (buildings.Count == 0)
        {
            _hotspotList.Children.Add(new Label { Text = "No hotspots to show.", TextColor = Muted });
            return;
        }

        var maxCount = Math.Max(1, buildings.Max(item => item.Count));
        foreach (var building in buildings.OrderByDescending(item => item.Count))
        {
            var color = CampusMapDrawable.ColorFor(building.Count, maxCount);
            _hotspotList.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E1E7EF"),
                Padding = new Thickness(10, 8),
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 9,
                    Children =
                    {
                        new BoxView { Color = color, WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, VerticalOptions = LayoutOptions.Center },
                        Place(new Label { Text = building.Name, FontSize = 13, TextColor = Ink, VerticalOptions = LayoutOptions.Center }, 1, 0),
                        Place(new Label { Text = building.Count.ToString(), FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ink, VerticalOptions = LayoutOptions.Center }, 2, 0)
                    }
                }
            });
        }
    }

    private static T Place<T>(T view, int column, int row) where T : View
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        return view;
    }
}
