using System.Numerics;
using System.Text;
using Fasterflect;

namespace FrostEvo.Core.Networking.Transmission;

public class NetMsg : IDisposable
{
    public Encoding Encoding { get; private set; } = Encoding.ASCII;
    private static bool NetMsgInitialized { get; set; } = false;
    private static Dictionary<MsgType, Type> NetMsgs { get; } = new();
    private const int BytesPerDumpLine = 16;
    public BinaryReader Reader { get; set; }
    public BinaryWriter Writer { get; set; }

    public bool ProtoBuff { get; set; } = false;
    public virtual MsgType Type { get; set; }

    protected NetMsg()
    {
    }

    public NetMsg(byte[] bytes)
    {
        if (!NetMsgInitialized)
            InitializeNetMsgs();

        Reader = new BinaryReader(new MemoryStream(bytes));
    }

    public void Dispose()
    {
        Reader?.Dispose();
        Writer?.Dispose();
    }

    #region ReadMethods

    public virtual void Read()
    {
    }

    public Vector3 ReadVector3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    public float ReadFloat() => Reader.ReadSingle();
    public byte ReadByte() => Reader.ReadByte();
    public byte[] ReadBytes(int amount) => Reader.ReadBytes(amount);

    public short ReadInt16() => Reader.ReadInt16();
    public ushort ReadUInt16() => Reader.ReadUInt16();

    public int ReadInt32() => Reader.ReadInt32();
    public uint ReadUInt32() => Reader.ReadUInt32();

    public long ReadInt64() => Reader.ReadInt64();
    public ulong ReadUInt64() => Reader.ReadUInt64();
    public string ReadStringWithHeader() => Encoding.GetString(Reader.ReadBytes(ReadByte()));
    public string ReadBigStringWithHeader() => Encoding.GetString(Reader.ReadBytes(ReadInt16()));

    public string[] ReadStringWithHeaderList()
    {
        var amount = Reader.ReadByte();
        var strings = new string[amount];
        for (var i = 0; i < amount; i++) strings[i] = ReadStringWithHeader();
        return strings;
    }

    public string ReadFixedString(int length)
    {
        return Encoding.GetString(Reader.ReadBytes(length)).TrimEnd('\0');
    }

    #endregion

    #region WriteMethods

    public virtual void Build()
    {
    }

    public void WriteVector3(Vector3 vec)
    {
        WriteFloat(vec.X);
        WriteFloat(vec.Y);
        WriteFloat(vec.Z);
    }

    public void WriteFloat(float value) => Writer.Write(value);

    public void WriteByte(byte value) => Writer.Write(value);
    public void WriteBytes(byte[] value) => Writer.Write(value);

    public void WriteFixedBytes(byte[] value, int length)
    {
        if (value == null)
            return;
        var data = new byte[length];
        Array.Copy(value, 0, data, 0, value.Length);
        Writer.Write(data);
    }

    public void WriteInt16(short value) => Writer.Write(value);
    public void WriteUInt16(ushort value) => Writer.Write(value);

    public void WriteInt32(int value) => Writer.Write(value);
    public void WriteUInt32(uint value) => Writer.Write(value);


    public void WriteInt64(long value) => Writer.Write(value);
    public void WriteUInt64(ulong value) => Writer.Write(value);

    public void WriteStringWithHeader(string value)
    {
        if (value == null)
            return;
        Writer.Write((byte) value.Length);
        Writer.Write(Encoding.GetBytes(value));
    }

    public void WriteBigStringWithHeader(string value)
    {
        if (value == null)
            return;
        Writer.Write((short) value.Length);
        Writer.Write(Encoding.GetBytes(value));
    }

    public void WriteStringWithHeaderList(params string[] strings)
    {
        Writer.Write((byte) strings.Length);
        foreach (var s in strings)
            WriteStringWithHeader(s);
    }

    public void WriteFixedString(string value, int length)
    {
        if (value == null)
            return;
        Writer.Write(value.PadRight(length, '\0').ToCharArray());
    }

    #endregion

    #region HelperMethods

    private byte[] GetBuffer()
    {
        if (Reader?.BaseStream is MemoryStream memoryStream && memoryStream.Length > 0)
            return memoryStream.ToArray();
        return Array.Empty<byte>();
    }

    protected void Seek(long offset, SeekOrigin seekOrigin)
    {
        Writer?.BaseStream?.Seek(offset, seekOrigin);
        Reader?.BaseStream?.Seek(offset, seekOrigin);
    }

    public string Dump()
    {
        return Dump(GetBuffer());
    }

    public static string Dump(byte[] buffer)
    {
        var lines = (BitConverter.ToUInt16(buffer, 0) + BytesPerDumpLine - 1) / BytesPerDumpLine;
        var size = 72 +
                   72 +
                   lines * 9 +
                   lines * 3 * BytesPerDumpLine +
                   lines * 1 * BytesPerDumpLine;

        int size1 = BitConverter.ToUInt16(buffer, 0);
        var builder = new StringBuilder(lines);

        // header
        builder.AppendLine("      00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f 0123456789abcdef");
        builder.AppendLine("    +------------------------------------------------ ----------------");
        for (var i = 0; i < size1; i += BytesPerDumpLine)
        {
            builder.Append(i.ToString("x3"));
            builder.Append(" | ");

            // create byte display
            for (var j = i; j < i + BytesPerDumpLine; j++)
            {
                var s = "   ";
                if (j < size1) s = buffer[j].ToString("x2") + " ";

                builder.Append(s);
            }

            builder.Append(' ');

            // create char representation
            for (var j = i; j < i + BytesPerDumpLine; j++)
            {
                var c = ' ';
                if (j < size1)
                {
                    c = (char) buffer[j];
                    if (c < ' ' || c >= 127) c = '.';
                }

                builder.Append(c);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    #endregion


    private void InitializeNetMsgs()
    {
        NetMsgInitialized = true;
        if (NetMsgs.Count != 0)
            return;
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes()
                .Where(y => y.IsSubclassOf(typeof(NetMsg)) && !y.IsAbstract));

        foreach (var t in types)
        {
            var msg = (NetMsg) t.CreateInstance();
            if (!NetMsgs.ContainsKey(msg.Type))
                NetMsgs.Add(msg.Type, t);
        }
    }

    private static NetMsg GetMsg(MsgType msgType)
    {
        if (NetMsgs.TryGetValue(msgType, out var type))
            return (NetMsg) type.CreateInstance();
        return null;
    }

    public MsgType GetMsgType()
    {
        ReadUInt16(); //Size
        Type = (MsgType) ReadUInt16();
        return Type;
    }

    public NetMsg PrepareMsg()
    {
        GetMsgType();
        var msg = GetMsg(Type);
        if (msg == null)
            return null;
        msg.Reader = Reader;
        return msg;
    }

    public T ReadMsg<T>() where T : NetMsg
    {
        try
        {
            if (this is not T msg)
                return null;

            msg.Read();
            return msg;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public byte[] WriteMsg(byte[] suffix)
    {
        try
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort) 0);
            writer.Write((ushort) Type);
            Writer = writer;
            Build();
            stream.Position = 0;
            writer.Write((ushort) stream.Length);
            stream.Position = stream.Length;
            if (suffix != null)
                writer.Write(suffix);
            return stream.ToArray();
        }
        catch (Exception)
        {
            throw new InvalidOperationException("NetMsg - BuildMsg()");
        }
    }
}