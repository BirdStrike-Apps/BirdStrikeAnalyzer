using Microsoft.Extensions.DependencyInjection;

namespace BirdStrikeAnalyzer;

public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        Title = "BirdStrike Analyzer";
        FlyoutBehavior = FlyoutBehavior.Flyout;
        FlyoutBackgroundColor = Colors.White;
        Routing.RegisterRoute(nameof(DetailsPage), typeof(DetailsPage));

        SetItemTemplate(this, new DataTemplate(() =>
        {
            var label = new Label
            {
                Padding = new Thickness(24, 14),
                FontSize = 18,
                TextColor = Color.FromArgb("#101820"),
                VerticalOptions = LayoutOptions.Center
            };
            label.SetBinding(Label.TextProperty, "Title");
            return label;
        }));

        SetTitleView(this, new Label
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Colors.White,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = "Bird", FontSize = 30, TextColor = Colors.White },
                    new Span { Text = "Strike", FontSize = 30, TextColor = Colors.White, FontAttributes = FontAttributes.Bold | FontAttributes.Italic }
                }
            }
        });

        Items.Add(MakeItem("Home", "home", () => services.GetRequiredService<MainPage>()));
        Items.Add(MakeItem("Map View", "mapview", () => services.GetRequiredService<MapViewPage>()));
        Items.Add(MakeItem("Bird Strikes", "birdstrikes", () => services.GetRequiredService<BirdStrikesPage>()));
        Items.Add(MakeItem("Analyzer", "analyzer", () => services.GetRequiredService<AnalyzerPage>()));
        Items.Add(MakeItem("Buildings", "buildings", () => new BuildingManagementPage()));
        Items.Add(MakeItem("Doors", "doors", () => new DoorManagementPage()));
        
    }

    private static FlyoutItem MakeItem(string title, string route, Func<Page> createPage)
    {
        var item = new FlyoutItem { Title = title, Route = route };
        item.Items.Add(new ShellContent
        {
            Title = title,
            Route = route,
            ContentTemplate = new DataTemplate(createPage)
        });
        return item;
    }
}

