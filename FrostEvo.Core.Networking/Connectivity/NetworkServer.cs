using System.Net;
using System.Net.Sockets;
using FrostEvo.Core.Networking.Security;
using FrostEvo.Core.Services;
using Microsoft.Extensions.Logging;

namespace FrostEvo.Core.Networking.Connectivity;

public class NetworkServer : ITransientService
{
    public NetworkServer(ILogger<NetworkServer> logger)
    {
        Logger = logger;
    }

    internal readonly ILogger<NetworkServer> Logger;
    private Socket Socket { get; set; }
    public EndPoint EndPoint { get; set; }
    public BufferManager BufferManager { get; set; }
    private FloodProtection FloodProtection { get; set; }
    public Semaphore AcceptanceSemaphore { get; set; }
    public CancellationTokenSource ShutdownToken { get; set; }
    public NetworkEvents.ClientConnection Connected { get; set; }
    public NetworkEvents.ClientReceive Received { get; set; }
    public NetworkEvents.ClientConnection Disconnected { get; set; }
    public byte[] Footer { get; set; }

    public Task<bool> Init(string ipAddress, int port,
        NetworkEvents.ClientConnection connected,
        NetworkEvents.ClientReceive received,
        NetworkEvents.ClientConnection disconnected,
        int bufferSize = 1024,
        byte footerLen = 0,
        int maxConnections = 1000,
        int backlog = 100,
        bool delay = false,
        bool fragment = false)
    {
        try
        {
            EndPoint = new IPEndPoint(ipAddress is "localhost" or "127.0.0.1" or "0.0.0.0" ? IPAddress.Any : Dns.GetHostEntryAsync(ipAddress).Result.AddressList.First(), port);
            Connected = connected;
            Received = received;
            Disconnected = disconnected;
            Footer = new byte[footerLen];
            BufferManager = new BufferManager(bufferSize);
            FloodProtection = new FloodProtection(30, 15);
            AcceptanceSemaphore = new Semaphore(maxConnections, maxConnections);
            ShutdownToken = new CancellationTokenSource();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = new LingerOption(false, 0),
                NoDelay = !delay,
                DontFragment = !fragment
            };
            Socket?.Bind(EndPoint);
            Socket?.Listen(backlog);
            Task.Run(Accept);
            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "NetworkServer - Init()");
            return Task.FromResult(false);
        }
    }

    public Task Stop()
    {
        try
        {
            ShutdownToken.Cancel();
            if (Socket.IsBound)
                Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "NetworkServer - Stop()");
            return Task.FromException(e);
        }
    }
    private async Task Accept()
    {
        try
        {
            while (Socket.IsBound && !ShutdownToken.IsCancellationRequested)
            {
                if (!AcceptanceSemaphore.WaitOne(TimeSpan.FromSeconds(5)))
                    continue;

                
                var client = new NetworkActor(this, await Socket.AcceptAsync());
                if (!FloodProtection.Authenticate(client.ToString()))
                {
                    await client.Disconnect();
                    continue;
                }
                Connected?.Invoke(client).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
           Logger.LogError(e, "NetworkServer - Accept()");
        }
    }
}