using Undefined.Verifying;

namespace Undefined.Serializer.Buffers;

public sealed class Buffer : IDisposable
{
    private byte[] _buffer;
    private DataConverter _converter;

    public bool IsResizable { get; }
    public int Length => _buffer.Length;

    public DataConverter Converter
    {
        get => _converter;
        set
        {
            Verify.NotNull(value);
            _converter = value;
        }
    }

    public Buffer(int size, bool isResizable, DataConverter? converter = null)
    {
        _converter = converter ?? DataConverter.GetDefault();
        Verify.Positive(size);
        IsResizable = isResizable;
        _buffer = new byte[size];
    }

    public Buffer(byte[] buffer, bool isResizable = false)
    {
        _buffer = buffer;
        IsResizable = isResizable;
    }

    public Buffer(Buffer buffer, bool isResizable = false)
    {
        _buffer = new byte[buffer.Length];
        Array.Copy(buffer._buffer, _buffer, _buffer.Length);
        IsResizable = isResizable;
    }

    public void Dispose()
    {
        _buffer = null;
    }

    public void Expand(int size) => Resize(Length + size);
    public bool TryExpand(int size) => TryResize(Length + size);

    public void Resize(int newSize)
    {
        Verify.Argument(IsResizable);
        Verify.Positive(newSize);
        Array.Resize(ref _buffer, newSize);
    }

    public bool TryResize(int newSize)
    {
        if (!IsResizable) return false;
        if (newSize < 1) return false;
        Array.Resize(ref _buffer, newSize);
        return true;
    }

    public Buffer Copy() => new(this);

    public byte[] GetBuffer() => _buffer;
}