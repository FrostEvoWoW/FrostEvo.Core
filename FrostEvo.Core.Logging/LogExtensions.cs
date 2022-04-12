using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FrostEvo.Core.Logging;

public static class LogExtensions
{
    public static Task AddSerilog(this IServiceCollection services, string logger = "Main")
    {
        Log.Logger = LogManager.CreateLogger(logger);
        services.AddLogging(x => x.AddSerilog());
        return Task.CompletedTask;
    }
}