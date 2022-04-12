using System.Net;
using System.Net.Sockets;
using FrostEvo.Core.Networking.Security.Cryptography;
using FrostEvo.Core.Networking.Transmission;
using Microsoft.Extensions.Logging;

namespace FrostEvo.Core.Networking.Connectivity;

public class NetworkActor
{
    public NetworkActor(NetworkServer networkServer, Socket socket)
    {
        Socket = socket;
        Server = networkServer;
        Buffer = Server.BufferManager.Rent();
        Socket.ReceiveBufferSize = Socket.SendBufferSize = Buffer.Length;
        MsgBuffer = new CircularBuffer<byte>(Buffer.Length * 2, true);
        ShutdownToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken(false),
            Server.ShutdownToken.Token);
    }
    private Socket Socket { get; set; }
    private NetworkServer Server { get; set; }
    private Memory<byte> Buffer { get; set; }
    private CircularBuffer<byte> MsgBuffer { get; set; }
    public INetworkCipher Cipher { get; set; }
    private CancellationTokenSource ShutdownToken { get; set; }
    private EndPoint EndPoint => Socket?.RemoteEndPoint;
    public override string ToString() => EndPoint?.ToString()?.Split(':')[0];
    public INetworkActor Owner { get; set; }
    public bool Connected => Socket.Connected;

    public async Task ReceiveAsync()
    {
        try
        {
            while (Socket.Connected && !ShutdownToken.IsCancellationRequested)
            {
                try
                {
                    var receiveOp =
                        await Socket.ReceiveAsync(Buffer[..], SocketFlags.None, ShutdownToken.Token);

#if !DEBUG
                    ShutdownToken.CancelAfter(TimeSpan.FromSeconds(30));
#endif

                    if (receiveOp <= 0)
                        break;

                    Cipher?.Decrypt(Buffer[..receiveOp].Span, receiveOp);
                    SplitProcess(Buffer[..receiveOp].ToArray());
                }
                catch (OperationCanceledException e)
                {
                    Server.Logger.LogError(e, "NetworkActor - ReceiveAsync()");
                    await Disconnect();
                    break;
                }
                catch (Exception e)
                {
                    Server.Logger.LogError(e, "NetworkActor - ReceiveAsync()");
                    await Disconnect();
                    break;
                }
            }

            await Disconnect();
        }
        catch (Exception e)
        {
            Server.Logger.LogError(e, "NetworkActor - ReceiveAsync()");
            await Disconnect();
        }
    }

    public async Task SendAsync(byte[] packet)
    {
        if (!Socket.Connected || ShutdownToken.IsCancellationRequested)
            return ;

        try
        {
            Cipher?.Encrypt(packet, packet.Length);
            await Socket?.SendAsync(packet, SocketFlags.None);
        }
        catch (SocketException e)
        {
            Server.Logger.LogError(e, "NetworkActor - SendAsync()");
            await Disconnect();
        }
    }

    private void SplitProcess(byte[] buffer)
    {
        try
        {
            MsgBuffer.Put(buffer);
            for (var i = 0; i < 5; i++)
            {
                if (MsgBuffer.Size < 2)
                    break;

                var msgLen = BitConverter.ToUInt16(MsgBuffer.Peek(2), 0);

                var footerLen = Server?.Footer?.Length;
                if (msgLen == 0 || MsgBuffer.Size < msgLen + footerLen)
                    break;

                var msg = MsgBuffer.Get(msgLen + footerLen.GetValueOrDefault(0)).Take(msgLen).ToArray();
                Server?.Received?.Invoke(this, new NetMsg(msg)).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            Server?.Logger.LogError(e, "NetworkActor - SplitProcess()");
            Disconnect();
        }
    }

    public Task Disconnect()
    {
        try
        {
            ShutdownToken.Cancel();
            if (Socket.IsBound)
                Socket?.Shutdown(SocketShutdown.Both);
            Socket?.Disconnect(false);
            Socket?.Close();
            Server?.AcceptanceSemaphore.Release();
            Server?.BufferManager.Return(Buffer);
            Server?.Disconnected?.Invoke(this).ConfigureAwait(false);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Server?.Logger.LogError(e, "NetworkActor - Disconnect()");
        }
        return Task.CompletedTask;
    }

    public bool TryGetBytesWithLength(out byte[] data)
    {
        if (TryGetDataUInt(out var value))
            return TryGetData((int) value, out data);

        data = null;
        return false;
    }

    public bool TryGetDataUInt(out uint value)
    {
        try
        {
            if (TryGetData(4, out var data))
            {
                value = BitConverter.ToUInt32(data, 0);
                return true;
            }
        }
        catch (Exception)
        {
            //ignore
        }

        value = 0;
        return false;
    }
    public bool TryGetData(int len, out byte[] data, int timeout = 3000)
    {
        try
        {
            data = new byte[len];
            var ar = Socket.BeginReceive(data, 0, data.Length, SocketFlags.None, null, null);
            if (ar == null)
                return false;
            ar.AsyncWaitHandle.WaitOne(timeout);
            var recv = Socket.EndReceive(ar);
            if (!ar.IsCompleted)
                return false;
            if (recv == len)
            {
                Cipher?.Decrypt(data, data.Length);
                return true;
            }
        }
        catch (Exception)
        {
            //ignore
        }

        data = null;
        return false;
    }
}