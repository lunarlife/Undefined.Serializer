namespace Undefined.Serializer.Converters;

public unsafe interface ICompressibleConverter : IConverterBase
{
    public void Serialize(object o, ref byte* buffer, bool compressed);
    public object? Deserialize(Type type, ref byte* buffer, bool compressed);
    public int GetSize(object o, bool compressed);
}

public unsafe interface ICompressibleConverter<T> : ICompressibleConverter
{
    Type IConverterBase.ImplementationType => typeof(T);
    int ICompressibleConverter.GetSize(object o, bool compressed) => GetSize((T)o, compressed);

    void ICompressibleConverter.Serialize(object o, ref byte* buffer, bool compressed) =>
        Serialize((T)o, ref buffer, compressed);

    object? ICompressibleConverter.Deserialize(Type type, ref byte* buffer, bool compressed) =>
        Deserialize(type, ref buffer, compressed);

    public void Serialize(T o, ref byte* buffer, bool compressed);
    public int GetSize(T o, bool compressed);

    new T? Deserialize(Type type, ref byte* buffer, bool compressed);
}