namespace BirdStrikeAnalyzer;

public sealed class AboutPage : ContentPage
{
    public AboutPage()
    {
        Title = "About";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    UiStyle.Heading("About"),
                    UiStyle.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Three-tier starter", FontSize = 20, FontAttributes = FontAttributes.Bold },
                            UiStyle.Divider(),
                            new Label { Text = "The UI is in this project’s root. BusinessLogic, Database, IDatabase, and the Supabase model are together under Model/.", FontSize = 14 }
                        }
                    })
                }
            }
        };
    }
}
