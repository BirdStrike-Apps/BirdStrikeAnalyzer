
using Microsoft.Maui.Controls;

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

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 15,
            Children =
            {
                new Label
                {
                    Text = "Building and Door Management",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold
                },

                new Label
                {
                    Text = "Manage buildings and their doors."
                },

                addBuildingButton,
                addDoorButton
            }
        };
    }
}
