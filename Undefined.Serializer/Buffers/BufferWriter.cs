using Undefined.Verifying;

namespace Undefined.Serializer.Buffers;

public class BufferWriter
{
    private int _position;


    public int Position
    {
        get => _position;
        set
        {
            Verify.Array(value, Buffer.Length,
                $"Invalid value {value}. Position must be between 0 and {Buffer.Length}.");
            _position = value;
        }
    }

    public int Left => Buffer.Length - Position;

    public Buffer Buffer { get; }

    public BufferWriter(Buffer buffer)
    {
        Buffer = buffer;
    }

    public int Write(object o, bool compressed)
    {
        var size = Buffer.Converter.SizeOf(o, compressed);
        if (Buffer.IsResizable && Left < size)
            Buffer.Resize(Buffer.Length + (size - Left));
        else
            Verify.Argument(Left >= size, "Buffer has no free memory.");

        var write = Buffer.Converter.SerializeUnsafe(o, Buffer.GetBuffer(), _position, compressed);
        Position += write;
        return write;
    }


    public int Write(byte value)
    {
        Verify.ArgumentFunc(Left > 0, "Buffer has no free memory.", () => Buffer.TryExpand(1));
        return Buffer.GetBuffer()[_position++] = value;
    }

    public int Write(byte[] value, int offset, int length)
    {
        Verify.Array(value, offset);
        Verify.Array(value, offset + length);
        if (Buffer.IsResizable && Left < length) Buffer.Resize(Buffer.Length + (length - Left));
        else
            Verify.Argument(Left >= length);

        Array.Copy(value, offset, Buffer.GetBuffer(), _position, length);
        _position += length;
        return length;
    }

    public int Write(byte[] value, int length) => Write(value, 0, length);
    public int Write(byte[] value) => Write(value, 0, value.Length);
}