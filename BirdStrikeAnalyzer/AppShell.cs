using Microsoft.Extensions.DependencyInjection;

namespace BirdStrikeAnalyzer;

public sealed class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        Title = "ThreeTierMauiSample";
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Routing.RegisterRoute(nameof(DetailsPage), typeof(DetailsPage));

        var tabs = new TabBar();
        var homeTab = new Tab { Title = "Home" };
        homeTab.Items.Add(new ShellContent
        {
            Title = "Home",
            Route = "home",
            ContentTemplate = new DataTemplate(() => services.GetRequiredService<MainPage>())
        });
        var aboutTab = new Tab { Title = "About" };
        aboutTab.Items.Add(new ShellContent
        {
            Title = "About",
            Route = "about",
            ContentTemplate = new DataTemplate(() => services.GetRequiredService<AboutPage>())
        });
        tabs.Items.Add(homeTab);
        tabs.Items.Add(aboutTab);
        Items.Add(tabs);
    }
}
