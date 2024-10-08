namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class EnumConverter : ICompressibleConverter<Enum>
{
    public DataConverter Converter { get; init; }


    public void Serialize(Enum o, ref byte* buffer, bool compressed) =>
        Converter.Serialize(Convert.ChangeType(o, o.GetTypeCode()), ref buffer, compressed);

    public Enum? Deserialize(Type type, ref byte* buffer, bool compressed) =>
        (Enum)Enum.ToObject(type,
            Converter.Deserialize(Enum.GetUnderlyingType(type), ref buffer)!);

    public int GetSize(Enum value, bool compressed) =>
        Converter.SizeOf(Convert.ChangeType(value, value.GetTypeCode()), compressed);
}