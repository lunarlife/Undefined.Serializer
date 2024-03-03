namespace Undefined.Serializer.Converters;

public abstract unsafe class PrimitiveConverter<T> : IConverter<T> where T : unmanaged
{
    public DataConverter Converter { get; init; }
    private readonly int _fSize = sizeof(T);


    public void Serialize(T o, ref byte* buffer)
    {
        *(T*)buffer = o;
        buffer += _fSize;
    }

    public T Deserialize(Type type, ref byte* buffer)
    {
        var value = *(T*)buffer;
        buffer += _fSize;
        return value;
    }

    public int GetSize(T value) => _fSize;
}

public abstract unsafe class PrimitiveCompressibleConverter<T> : ICompressibleConverter<T> where T : unmanaged
{
    public DataConverter Converter { get; init; }
    private readonly int _fSize = sizeof(T);


    public void Serialize(T o, ref byte* buffer, bool compressed)
    {
        if (compressed) Serialize(o, ref buffer);
        else
        {
            *(T*)buffer = o;
            buffer += _fSize;
        }
    }

    public T Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        if (compressed) return Deserialize(type, ref buffer);
        var value = *(T*)buffer;
        buffer += _fSize;
        return value;
    }

    protected abstract void Serialize(T o, ref byte* buffer);
    protected abstract T Deserialize(Type type, ref byte* buffer);
    protected abstract int GetSize(T value);

    public int GetSize(T value, bool compressed) => compressed ? GetSize(value) : _fSize;
}