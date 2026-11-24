using BirdStrikeAnalyzer.Model;
using System.Globalization;
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
                    UiStyle.Heading("Door Management"),

                   _buildingPicker,
addDoorButton,

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
                var editButton = new Button
                {
                    Text = "Edit Door",
                    HorizontalOptions = LayoutOptions.Start
                };

                editButton.Clicked += async (sender, e) =>
                {
                    editButton.IsEnabled = false;

                    try
                    {
                        await EditDoorAsync(door);
                    }
                    finally
                    {
                        editButton.IsEnabled = true;
                    }
                };

                _doorList.Children.Add(editButton);
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
    private async Task AddDoorAsync()
    {
        if (_buildingPicker.SelectedItem is not string buildingName)
        {
            await DisplayAlert("Error", "Select a building first.", "OK");
            return;
        }

        try
        {
            var door = await DisplayPromptAsync(
                "Add Door", "Enter door number:");

            if (string.IsNullOrWhiteSpace(door))
                return;

            var latitudeInput = await DisplayPromptAsync(
                "Add Door", "Enter latitude:");

            if (latitudeInput is null)
                return;

            var longitudeInput = await DisplayPromptAsync(
                "Add Door", "Enter longitude:");

            if (longitudeInput is null)
                return;

            if (!double.TryParse(latitudeInput,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double latitude) ||
                !double.TryParse(longitudeInput,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double longitude))
            {
                await DisplayAlert("Error", "Invalid coordinates.", "OK");
                return;
            }

            await _businessLogic.AddDoorAsync(
                buildingName, door, latitude, longitude);

            await LoadDoorsAsync();

            await DisplayAlert(
                "Success", "Door added successfully.", "OK");
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
            await DisplayAlert("Error", "Unable to add door.", "OK");
        }
    }

    private async Task EditDoorAsync(BuildingDoors door)
    {
        try
        {
            // Edit the current door number.
            var doorNumber = await DisplayPromptAsync(
                "Edit Door",
                "Enter door number:",
                initialValue: door.Door);

            if (string.IsNullOrWhiteSpace(doorNumber))
                return;

            // Edit the current latitude.
            var latitudeInput = await DisplayPromptAsync(
                "Edit Door",
                "Enter latitude:",
               initialValue: Convert.ToString(
    door.Latitude, CultureInfo.InvariantCulture));

            if (latitudeInput is null)
                return;

            // Edit the current longitude.
            var longitudeInput = await DisplayPromptAsync(
                "Edit Door",
                "Enter longitude:",
               initialValue: Convert.ToString(
    door.Longitude, CultureInfo.InvariantCulture));

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

            // Update the existing door.
            await _businessLogic.EditDoorAsync(
                door.BuildingDoorId,
                doorNumber,
                latitude,
                longitude);

            // Refresh the door list.
            await LoadDoorsAsync();

            await DisplayAlert(
                "Success",
                "Door updated successfully.",
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
                "Error", "Unable to update door.", "OK");
        }
    }




}