
using Microsoft.Maui.Controls;

namespace BirdStrikeAnalyzer;

public class DoorManagementPage : ContentPage
{
    public DoorManagementPage()
    {
        Title = "Door Management";

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 15,
            Children =
            {
                new Label
                {
                    Text = "Door Management",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold
                },

                new Label
                {
                    Text = "View and manage doors for each building."
                },

                new Button
                {
                    Text = "Add Door"
                }
            }
        };
    }
}
