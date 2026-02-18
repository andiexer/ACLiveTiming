using System.Buffers.Binary;
using System.Text;

namespace Devlabs.AcTiming.Infrastructure.AcServer;

public ref struct AcPacketReader
{
    private ReadOnlySpan<byte> _data;
    private int _pos;

    public AcPacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos = 0;
    }

    public byte ReadByte() => _data[_pos++];

    public int ReadInt32()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return value;
    }

    public ushort ReadUInt16()
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_data[_pos..]);
        _pos += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return value;
    }

    public float ReadFloat()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data[_pos..]);
        _pos += 4;
        return value;
    }

    public bool ReadBool() => ReadByte() != 0;

    /// <summary>
    /// Reads a length-prefixed UTF-32LE string (AC protocol format).
    /// Length prefix is a single byte containing character count.
    /// Each character is 4 bytes (UTF-32LE).
    /// </summary>
    public string ReadStringW()
    {
        var charCount = ReadByte();
        var byteCount = charCount * 4;

        if (byteCount == 0)
            return string.Empty;

        var value = Encoding.UTF32.GetString(_data.Slice(_pos, byteCount));
        _pos += byteCount;
        return value;
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string.
    /// Length prefix is a single byte containing byte count.
    /// </summary>
    public string ReadString()
    {
        var length = ReadByte();

        if (length == 0)
            return string.Empty;

        var value = Encoding.UTF8.GetString(_data.Slice(_pos, length));
        _pos += length;
        return value;
    }

    public AcVector3 ReadVector3() => new(ReadFloat(), ReadFloat(), ReadFloat());

    public int Remaining => _data.Length - _pos;
}
