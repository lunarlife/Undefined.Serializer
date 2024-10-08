using Undefined.Verifying;

namespace Undefined.Serializer.Buffers;

public class BufferReader
{
    private int _position;


    public int Position
    {
        get => _position;
        set
        {
            Verify.Array(value, Buffer.Length);
            _position = value;
        }
    }

    public int Left => Buffer.Length - Position;

    public Buffer Buffer { get; }

    public BufferReader(Buffer buffer)
    {
        Buffer = buffer;
    }

    public byte Read()
    {
        Verify.Argument(Left > 0, "Buffer has no free memory.");
        return Buffer.GetBuffer()[_position++];
    }

    public T? Read<T>()
    {
        Verify.Argument(Left > 0, "Buffer has no free memory.");
        var read = Buffer.Converter.Deserialize<T>(Buffer.GetBuffer(), _position, out var length);
        _position += length;
        return read;
    }

    public object? Read(Type type)
    {
        Verify.Argument(Left > 0, "Buffer has no free memory.");
        var read = Buffer.Converter.Deserialize(type, Buffer.GetBuffer(), _position, out var length);
        _position += length;
        return read;
    }

    public byte[] Read(int length)
    {
        Verify.Argument(Left >= length);
        Verify.Positive(length);
        var array = new byte[length];
        Array.Copy(Buffer.GetBuffer(), _position, array, 0, length);
        _position += length;
        return array;
    }
}