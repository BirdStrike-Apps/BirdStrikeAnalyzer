using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

public class DoorManagementPage : ContentPage
{
    private readonly BusinessLogic _businessLogic;

    private readonly Picker _buildingPicker = new()
    {
        Title = "Select a building"
    };

    private readonly VerticalStackLayout _doorList = new()
    {
        Spacing = 10
    };

    public DoorManagementPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;

        Title = "Door Management";

        // Load doors when a building is selected.
        _buildingPicker.SelectedIndexChanged += async (sender, e) =>
        {
            await LoadDoorsAsync();
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    UiStyle.Heading("Door Management"),

                    _buildingPicker,

                    UiStyle.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label
                            {
                                Text = "Doors",
                                FontSize = 20,
                                FontAttributes = FontAttributes.Bold
                            },

                            UiStyle.Divider(),

                            new Label
                            {
                                Text = "View and manage doors for each building.",
                                FontSize = 14
                            },

                            _doorList
                        }
                    }),

                    new Button { Text = "Add Door" }
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadBuildingsAsync();
    }

    private async Task LoadBuildingsAsync()
    {
        try
        {
            var buildings = await _businessLogic.GetBuildingsAsync();

            _buildingPicker.ItemsSource = buildings.ToList();

            if (buildings.Count > 0)
                _buildingPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlert(
                "Error",
                "Unable to load buildings.",
                "OK");
        }
    }

    private async Task LoadDoorsAsync()
    {
        _doorList.Children.Clear();

        if (_buildingPicker.SelectedItem is not string buildingName)
            return;

        try
        {
            var doors = await _businessLogic.GetDoorsAsync(
                buildingName);

            // Ignore outdated results if the selection changed.
            if (_buildingPicker.SelectedItem as string != buildingName)
                return;

            _doorList.Children.Clear();

            foreach (var door in doors)
            {
                _doorList.Children.Add(new Label
                {
                    Text = $"Door {door.Door}",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold
                });

                _doorList.Children.Add(new Label
                {
                    Text = $"Latitude: {door.Latitude}",
                    FontSize = 14
                });

                _doorList.Children.Add(new Label
                {
                    Text = $"Longitude: {door.Longitude}",
                    FontSize = 14
                });

                _doorList.Children.Add(UiStyle.Divider());
            }

            if (doors.Count == 0)
            {
                _doorList.Children.Add(new Label
                {
                    Text = "No doors found."
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlert(
                "Error",
                "Unable to load doors.",
                "OK");
        }
    }
}