using System.Text;

namespace Devlabs.AcTiming.Simulator;

/// <summary>
/// Writes binary packets in AC UDP protocol format.
/// </summary>
public class AcPacketWriter
{
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;

    public AcPacketWriter()
    {
        _writer = new BinaryWriter(_stream);
    }

    public AcPacketWriter WriteByte(byte value)
    {
        _writer.Write(value);
        return this;
    }

    public AcPacketWriter WriteUInt16(ushort value)
    {
        _writer.Write(value);
        return this;
    }

    public AcPacketWriter WriteUInt32(uint value)
    {
        _writer.Write(value);
        return this;
    }

    public AcPacketWriter WriteInt32(int value)
    {
        _writer.Write(value);
        return this;
    }

    public AcPacketWriter WriteFloat(float value)
    {
        _writer.Write(value);
        return this;
    }

    public AcPacketWriter WriteBool(bool value)
    {
        _writer.Write((byte)(value ? 1 : 0));
        return this;
    }

    /// <summary>
    /// Writes a length-prefixed UTF-32LE string (AC wide string format).
    /// </summary>
    public AcPacketWriter WriteStringW(string value)
    {
        var bytes = Encoding.UTF32.GetBytes(value);
        _writer.Write((byte)(bytes.Length / 4)); // char count
        _writer.Write(bytes);
        return this;
    }

    /// <summary>
    /// Writes a length-prefixed UTF-8 string.
    /// </summary>
    public AcPacketWriter WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _writer.Write((byte)bytes.Length);
        _writer.Write(bytes);
        return this;
    }

    public AcPacketWriter WriteVector3(float x, float y, float z)
    {
        _writer.Write(x);
        _writer.Write(y);
        _writer.Write(z);
        return this;
    }

    public byte[] ToArray()
    {
        _writer.Flush();
        return _stream.ToArray();
    }

    public void Reset()
    {
        _stream.SetLength(0);
        _stream.Position = 0;
    }
}
