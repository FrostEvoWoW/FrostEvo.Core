using System.Collections.Concurrent;
using FrostEvo.Core.Extensions;

namespace FrostEvo.Core.Networking.Security;

public sealed class FloodProtection
{
    public bool Active { get; set; }
    private readonly ConcurrentDictionary<string, long> _blockedConnections;
    private ConcurrentDictionary<string, int> _recentConnections;
    private readonly uint _maximumAttempts;
    private readonly uint _timeOut;
    public FloodProtection(uint maxConnPerMin, uint timeOut)
    {
        Active = true;
        _maximumAttempts = maxConnPerMin;
        _timeOut = timeOut;
        _recentConnections = new ConcurrentDictionary<string, int>();
        _blockedConnections = new ConcurrentDictionary<string, long>();

        Task.Run(async () =>
        {
            while (Active)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                _recentConnections = new ConcurrentDictionary<string, int>();
                foreach (var key in _blockedConnections.Keys)
                {
                    if (_blockedConnections[key] < Environment.TickCount)
                        _blockedConnections.TryRemove(key);
                }
            }
        });
    }

    public bool Authenticate(string ip)
    {
        if (_blockedConnections.TryGetValue(ip, out var connection))
            return false;

        if (_recentConnections.TryGetValue(ip, out _))
        {
            if (++_recentConnections[ip] <= _maximumAttempts) return true;
            _blockedConnections.TryAdd(ip, Environment.TickCount + _timeOut * 60000);
            _recentConnections.TryRemove(ip);
            return false;
        }
        else
            _recentConnections.TryAdd(ip, 1);
        return true;
    }
    public void ClearBlocked() => _blockedConnections.Clear();
}