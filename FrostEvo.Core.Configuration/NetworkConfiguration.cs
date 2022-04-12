namespace FrostEvo.Core.Configurations;

public class NetworkConfiguration
{
    public string ServerName { get; set; }
    public string IpAddress { get; set; }
    public ushort AuthPort { get; set; }
    public ushort WorldPort { get; set; }
    public byte Permission { get; set; }
    public int MaxConnectionAllowed { get; set; }
    public int BackLog { get; set; } 
    public bool Delay { get; set; }
    public bool Fragment { get; set; }
}