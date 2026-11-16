namespace BirdStrikeAnalyzer;

public sealed class DetailsPage : ContentPage
{
    public DetailsPage()
    {
        Title = "Details";
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 16,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "Navigation works", FontSize = 26, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "This page is opened with a registered .NET MAUI Shell route.", HorizontalTextAlignment = TextAlignment.Center },
                new Button { Text = "Back", Command = new Command(async () => await Shell.Current.GoToAsync("..")) }
            }
        };
    }
}
