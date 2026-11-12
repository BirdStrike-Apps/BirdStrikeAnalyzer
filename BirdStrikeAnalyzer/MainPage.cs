using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

public sealed class MainPage : ContentPage
{
    private readonly BusinessLogic _businessLogic;
    private readonly Label _connectionStatus = new()
    {
        Text = "Supabase has not been contacted. The app is ready to use.",
        TextColor = Color.FromArgb("#475467")
    };

    public MainPage(BusinessLogic businessLogic)
    {
        _businessLogic = businessLogic;
        Title = "Home";
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 18,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "MAUI + Supabase", FontSize = 28, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = "A basic .NET 10 app with UI, business, and database layers in one project.", HorizontalTextAlignment = TextAlignment.Center },
                _connectionStatus,
                MakeButton("Connect to Supabase", ConnectAsync),
                MakeButton("Open details", async () => await Shell.Current.GoToAsync(nameof(DetailsPage)))
            }
        };
    }

    private static Button MakeButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private async Task ConnectAsync()
    {
        _connectionStatus.Text = "Connecting…";
        try
        {
            var rows = await _businessLogic.GetSampleItemsAsync();
            _connectionStatus.Text = $"Connected. Read {rows.Count} sample row(s) from Supabase.";
        }
        catch (Exception ex)
        {
            _connectionStatus.Text = ex.Message;
        }
    }
}
