namespace BirdStrikeAnalyzer;

public sealed class DetailsPage : ContentPage
{
    public DetailsPage()
    {
        Title = "Details";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    UiStyle.Heading("Details"),
                    UiStyle.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Navigation works", FontSize = 20, FontAttributes = FontAttributes.Bold },
                            UiStyle.Divider(),
                            new Label { Text = "This page is opened with a registered .NET MAUI Shell route.", FontSize = 14 }
                        }
                    }),
                    new Button { Text = "Back", Command = new Command(async () => await Shell.Current.GoToAsync("..")) }
                }
            }
        };
    }
}
