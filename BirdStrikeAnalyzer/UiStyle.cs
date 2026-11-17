namespace BirdStrikeAnalyzer;

/// <summary>Small helpers so code-built pages share the card/heading look from BirdStrikeTheme.xaml.</summary>
internal static class UiStyle
{
    public static Border Card(View content)
    {
        var card = new Border { Content = content };
        if (Application.Current?.Resources.TryGetValue("Card", out var style) == true && style is Style cardStyle)
            card.Style = cardStyle;
        else
            card.BackgroundColor = Colors.White;
        return card;
    }

    public static Label Heading(string text, double size = 24) => new()
    {
        Text = text,
        FontSize = size,
        FontAttributes = FontAttributes.Bold
    };

    public static BoxView Divider() => new()
    {
        HeightRequest = 1,
        Color = Color.FromArgb("#C8C8C8"),
        HorizontalOptions = LayoutOptions.Fill
    };

    public static string Greeting()
    {
        var hour = DateTime.Now.Hour;
        return hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
    }
}
