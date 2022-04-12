namespace FrostEvo.Core.Configurations;

public class GameConfiguration
{
    public byte MaxLevel { get; set; }
    public double ExperienceRate { get; set; }
    public double ProfessionRate { get; set; }
    public double DropRate { get; set; }
    public double LifeRegenerationRate { get; set; }
    public double ManaRegenerationRate { get; set; }
    public bool GlobalAuction { get; set; }
    public uint SaveInterval { get; set; }
    public uint WeatherInterval { get; set; }
    public uint MapResolution { get; set; }
    public bool VMapsEnabled { get; set; }
    public bool VMapsLoSEnabled { get; set; }
    public bool VMapsHeightEnabled { get; set; }
    public string CommandPrefix { get; set; }
}