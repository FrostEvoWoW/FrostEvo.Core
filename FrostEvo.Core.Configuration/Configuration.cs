using FrostEvo.Core.Services;
using Microsoft.Extensions.Configuration;

namespace FrostEvo.Core.Configurations;

public class Configuration : ITransientService
{
    public DatabaseConfiguration DatabaseConfiguration { get; set; } = new();
    public NetworkConfiguration NetworkConfiguration { get; set; } = new();
    public GameConfiguration GameConfiguration { get; set; } = new();

    public Configuration() =>
        new ConfigurationBuilder()
            .AddJsonFile("Configuration.json", false, true)
            .Build()
            .Bind(this);

    public bool Valid =>
        DatabaseConfiguration != null &&
        NetworkConfiguration != null &&
        GameConfiguration != null;
}