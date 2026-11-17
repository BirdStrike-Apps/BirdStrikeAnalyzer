namespace BirdStrikeAnalyzer;

public class DoorManagementPage : ContentPage
{
    public DoorManagementPage()
    {
        Title = "Door Management";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    UiStyle.Heading("Door Management"),
                    UiStyle.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Doors", FontSize = 20, FontAttributes = FontAttributes.Bold },
                            UiStyle.Divider(),
                            new Label { Text = "View and manage doors for each building.", FontSize = 14 }
                        }
                    }),
                    new Button { Text = "Add Door" }
                }
            }
        };
    }
}
