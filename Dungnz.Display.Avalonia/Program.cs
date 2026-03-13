using Avalonia;
using Dungnz.Display.Avalonia;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog (same pattern as console app)
var logDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Dungnz", "Logs");
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(Path.Combine(logDir, "dungnz-avalonia-.log"),
                  rollingInterval: Serilog.RollingInterval.Day)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger("Dungnz.Avalonia");

logger.LogInformation("Dungnz Avalonia GUI starting...");

// Build and start Avalonia application
// The App.OnFrameworkInitializationCompleted will create MainWindow
// and start the game loop on a background thread
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
