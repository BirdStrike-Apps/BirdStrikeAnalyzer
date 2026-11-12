namespace BirdStrikeAnalyzer;

public sealed class AboutPage : ContentPage
{
    public AboutPage()
    {
        Title = "About";
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "Three-tier starter", FontSize = 26, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "The UI is in this project’s root. BusinessLogic, Database, IDatabase, and the Supabase model are together under Model/.", HorizontalTextAlignment = TextAlignment.Center }
            }
        };
    }
}
