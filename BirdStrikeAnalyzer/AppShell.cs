using Microsoft.Extensions.DependencyInjection;

namespace BirdStrikeAnalyzer;

public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        Title = "ThreeTierMauiSample";

        // Hamburger menu in the top bar instead of top tabs.
        FlyoutBehavior = FlyoutBehavior.Flyout;
        FlyoutBackgroundColor = Colors.White;
        Routing.RegisterRoute(nameof(DetailsPage), typeof(DetailsPage));

        // Dark, readable menu entries regardless of light/dark theme.
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

        // Top bar shows only the BirdStrike logo ("Bird" regular + "Strike" bold italic, white), left-aligned.
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
        Items.Add(MakeItem("Buildings", "buildings", () => new BuildingManagementPage()));
        Items.Add(MakeItem("Doors", "doors", () => new DoorManagementPage()));
        Items.Add(MakeItem("Bird Strikes", "birdstrikes", () => new BirdStrikesPage()));
        Items.Add(MakeItem("About", "about", () => services.GetRequiredService<AboutPage>()));
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
