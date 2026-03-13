using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Dungnz.Display.Avalonia;

/// <summary>
/// Helper class to configure and launch the Avalonia application.
/// </summary>
public static class AvaloniaAppBuilder
{
    /// <summary>
    /// Configures the Avalonia application builder.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Configured AppBuilder instance.</returns>
    public static AppBuilder Configure(string[] args)
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    /// <summary>
    /// Starts the Avalonia application and runs the game on a background thread.
    /// TODO: P3-P8 — wire game loop to background thread and display service to UI thread.
    /// </summary>
    /// <param name="builder">Configured AppBuilder.</param>
    public static void RunGame(this AppBuilder builder)
    {
        // Phase 2 stub: just launch the UI
        builder.StartWithClassicDesktopLifetime(Array.Empty<string>());
    }
}
