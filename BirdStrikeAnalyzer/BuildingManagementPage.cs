using BirdStrikeAnalyzer.Model;
using System.Globalization;
namespace BirdStrikeAnalyzer;

public class BuildingManagementPage : ContentPage
{
    private readonly BusinessLogic _businessLogic;

    private readonly VerticalStackLayout _buildingList = new()
    {
        Spacing = 10
    };

    public BuildingManagementPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;

        Title = "Building Management";

        var addBuildingButton = new Button
        {
            Text = "Add Building"
        };


        addBuildingButton.Clicked += async (sender, e) =>
        {
            addBuildingButton.IsEnabled = false;

            try
            {
                await AddBuildingAsync();
            }
            finally
            {
                addBuildingButton.IsEnabled = true;
            }
        };


        var addDoorButton = new Button
        {
            Text = "Add Door"
        };

        addDoorButton.Clicked += async (sender, e) =>
{
    addDoorButton.IsEnabled = false;

    try
    {
        await AddDoorAsync();
    }
    finally
    {
        addDoorButton.IsEnabled = true;
    }
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
                            new Label
                            {
                                Text = "Buildings",
                                FontSize = 20,
                                FontAttributes = FontAttributes.Bold
                            },

                            UiStyle.Divider(),

                            new Label
                            {
                                Text = "Manage buildings and their doors.",
                                FontSize = 14
                            },

                            _buildingList
                        }
                    }),

                    addBuildingButton,
                    addDoorButton
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

            _buildingList.Children.Clear();

            foreach (var building in buildings)
            {
                _buildingList.Children.Add(new Label
                {
                    Text = building,
                    FontSize = 16
                });
            }
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

    private async Task AddBuildingAsync()
    {
        try
        {
            // Get the building name.
            var buildingName = await DisplayPromptAsync(
                "Add Building",
                "Enter building name:");

            if (string.IsNullOrWhiteSpace(buildingName))
                return;

            // Get the first door number.
            var door = await DisplayPromptAsync(
                "Add Building",
                "Enter the first door number:",
                initialValue: "1");

            if (string.IsNullOrWhiteSpace(door))
                return;

            // Get latitude.
            var latitudeInput = await DisplayPromptAsync(
                "Location",
                "Enter latitude:",
                keyboard: Keyboard.Numeric);

            if (latitudeInput is null)
                return;

            // Get longitude.
            var longitudeInput = await DisplayPromptAsync(
                "Location",
                "Enter longitude:",
                keyboard: Keyboard.Numeric);

            if (longitudeInput is null)
                return;

            // Convert coordinates to numbers.
            if (!double.TryParse(latitudeInput,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double latitude) ||
                !double.TryParse(longitudeInput,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double longitude))
            {
                await DisplayAlert(
                    "Error",
                    "Please enter valid coordinates.",
                    "OK");

                return;
            }

            // Save through the BusinessLogic layer.
            await _businessLogic.AddBuildingAsync(
                buildingName,
                door,
                latitude,
                longitude);

            await DisplayAlert(
                "Success",
                "Building added successfully.",
                "OK");

            // Refresh the building list.
            await LoadBuildingsAsync();
        }
        catch (ArgumentException ex)
        {
            await DisplayAlert("Invalid Input", ex.Message, "OK");
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlert(
                "Error",
                "Unable to add building.",
                "OK");
        }
    }


private async Task AddDoorAsync()
{
    try
    {
        // Retrieve existing buildings.
        var buildings = await _businessLogic.GetBuildingsAsync();

        if (buildings.Count == 0)
        {
            await DisplayAlert("Error", "No buildings found.", "OK");
            return;
        }

        // Select a building.
        var buildingName = await DisplayActionSheet(
            "Select Building",
            "Cancel",
            null,
            buildings.ToArray());

        if (string.IsNullOrWhiteSpace(buildingName) ||
            buildingName == "Cancel")
            return;

        // Enter door number.
        var door = await DisplayPromptAsync(
            "Add Door",
            "Enter door number:");

        if (string.IsNullOrWhiteSpace(door))
            return;

        // Enter latitude.
        var latitudeInput = await DisplayPromptAsync(
            "Location",
            "Enter latitude:",
            keyboard: Keyboard.Numeric);

        if (latitudeInput is null)
            return;

        // Enter longitude.
        var longitudeInput = await DisplayPromptAsync(
            "Location",
            "Enter longitude:",
            keyboard: Keyboard.Numeric);

        if (longitudeInput is null)
            return;

        // Validate coordinates.
        if (!double.TryParse(latitudeInput,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double latitude) ||
            !double.TryParse(longitudeInput,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double longitude))
        {
            await DisplayAlert(
                "Error", "Invalid coordinates.", "OK");

            return;
        }

        // Save through BusinessLogic.
        await _businessLogic.AddDoorAsync(
            buildingName,
            door,
            latitude,
            longitude);

        await DisplayAlert(
            "Success",
            "Door added successfully.",
            "OK");
    }
    catch (ArgumentException ex)
    {
        await DisplayAlert("Invalid Input", ex.Message, "OK");
    }
    catch (InvalidOperationException ex)
    {
        await DisplayAlert("Error", ex.Message, "OK");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex);

        await DisplayAlert(
            "Error",
            "Unable to add door.",
            "OK");
    }
}





}