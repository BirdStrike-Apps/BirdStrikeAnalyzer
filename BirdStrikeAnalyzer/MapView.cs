using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

/// <summary>Expanded campus map view using the same hotspot drawable and sample data as Home.</summary>
public sealed class MapViewPage : ContentPage
{
    private static readonly Color PanelBlue = Color.FromArgb("#9CCCF8");
    private static readonly Color Ink = Color.FromArgb("#101820");
    private static readonly Color Muted = Color.FromArgb("#64748B");

    public MapViewPage()
    {
        Title = "Map View";
        BackgroundColor = Color.FromArgb("#F3F7FB");

        var map = new GraphicsView
        {
            Drawable = new CampusMapDrawable(HomeDashboardData.Buildings),
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
                Place(map, 0, 1)
            }
        };
        var mapPanel = new Border
        {
            BackgroundColor = PanelBlue,
            Stroke = Colors.Transparent,
            Padding = new Thickness(16),
            Content = mapPanelContent
        };

        var hotspotList = new VerticalStackLayout { Spacing = 8 };
        foreach (var building in HomeDashboardData.Buildings.OrderByDescending(item => item.Count))
        {
            var color = CampusMapDrawable.ColorFor(building.Count, HomeDashboardData.Buildings.Max(item => item.Count));
            hotspotList.Children.Add(new Border
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
                Place(new ScrollView { Content = hotspotList }, 0, 2)
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
                        new Label { Text = "Explore birdstrike report hotspots across campus.", FontSize = 14, TextColor = Muted }
                    }
                }
            }
        };

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

    private static T Place<T>(T view, int column, int row) where T : View
    {
        Grid.SetColumn(view, column);
        Grid.SetRow(view, row);
        return view;
    }
}
