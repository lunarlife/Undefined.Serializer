namespace Undefined.Serializer.Converters;

public interface IConverterBase
{
    public bool Inheritance => false;
    public DataConverter Converter { get; init; }
    public Type ImplementationType { get; }
}

public interface IConverter : IConverterBase
{
    public unsafe void Serialize(object o, ref byte* buffer);
    public unsafe object? Deserialize(Type type, ref byte* buffer);
    public int GetSize(object o);
}

public interface IConverter<T> : IConverter
{
    Type IConverterBase.ImplementationType => typeof(T);

    unsafe void IConverter.Serialize(object o, ref byte* buffer) =>
        Serialize((T)o, ref buffer);

    unsafe object? IConverter.Deserialize(Type type, ref byte* buffer) =>
        Deserialize(type, ref buffer);

    int IConverter.GetSize(object o) => GetSize((T)o);

    public int GetSize(T value);
    public unsafe void Serialize(T o, ref byte* buffer);
    public new unsafe T? Deserialize(Type type, ref byte* buffer);
}