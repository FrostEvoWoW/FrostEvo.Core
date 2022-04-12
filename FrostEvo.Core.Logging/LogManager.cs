using FrostEvo.Core.Configurations;
using FrostEvo.Core.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

namespace FrostEvo.Core.Logging;

public class LogManager : ISingletonService
{
    public ILogger Auth { get; set; } = CreateLogger("Auth");
    public ILogger World { get; set; } = CreateLogger("World");
    public ILogger Application { get; set; } = CreateLogger("Application");
    public ILogger Thread { get; set; } = CreateLogger("Thread");
    public ILogger Network { get; set; } = CreateLogger("Network");
    public ILogger Script { get; set; } = CreateLogger("Script");

    public static ILogger CreateLogger(string name)
    {
#if DEBUG
        var loglevel = LogEventLevel.Verbose;
#elif RELEASE
        var logLevel = LogEventLevel.Information;
#endif
        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(loglevel))
            .Enrich.WithExceptionDetails()
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: CreateConsoleTheme(),
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.MongoDBBson(configuration =>
            {
                configuration.SetConnectionString($"mongodb://localhost:27017/{new Configuration().DatabaseConfiguration.LogSchema}");
                configuration.SetBatchPeriod(TimeSpan.FromSeconds(1));
                configuration.SetCollectionName(name);
            })
            .CreateLogger();
    }

    private static SystemConsoleTheme CreateConsoleTheme() =>
        new(new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
        {
            [ConsoleThemeStyle.Text] = new() {Foreground = ConsoleColor.Yellow},
            [ConsoleThemeStyle.SecondaryText] = new() {Foreground = ConsoleColor.DarkCyan},
            [ConsoleThemeStyle.TertiaryText] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.Invalid] = new() {Foreground = ConsoleColor.Yellow},
            [ConsoleThemeStyle.Null] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.Name] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.String] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.Number] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.Boolean] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.Scalar] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.LevelVerbose] = new() {Foreground = ConsoleColor.Gray},
            [ConsoleThemeStyle.LevelDebug] = new() {Foreground = ConsoleColor.White},
            [ConsoleThemeStyle.LevelInformation] = new() {Foreground = ConsoleColor.DarkGreen},
            [ConsoleThemeStyle.LevelWarning] = new() {Foreground = ConsoleColor.DarkYellow},
            [ConsoleThemeStyle.LevelError] = new() {Foreground = ConsoleColor.Red},
            [ConsoleThemeStyle.LevelFatal] = new() {Foreground = ConsoleColor.DarkRed}
        });
}