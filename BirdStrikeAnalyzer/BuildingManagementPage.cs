namespace BirdStrikeAnalyzer;

public class BuildingManagementPage : ContentPage
{
    public BuildingManagementPage()
    {
        Title = "Building Management";

        var addBuildingButton = new Button
        {
            Text = "Add Building"
        };

        var addDoorButton = new Button
        {
            Text = "Add Door"
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    UiStyle.Heading("Building and Door Management"),
                    UiStyle.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Buildings", FontSize = 20, FontAttributes = FontAttributes.Bold },
                            UiStyle.Divider(),
                            new Label { Text = "Manage buildings and their doors.", FontSize = 14 }
                        }
                    }),
                    addBuildingButton,
                    addDoorButton
                }
            }
        };
    }
}
