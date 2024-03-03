namespace Undefined.Serializer.Converters.Default;

public sealed class ByteConverter : IConverter<byte>
{
    private const int F_SIZE = sizeof(byte);
    public DataConverter Converter { get; init; }
    public int GetSize(byte value) => F_SIZE;
    public unsafe void Serialize(byte o, ref byte* buffer) => *buffer++ = o;

    public unsafe byte Deserialize(Type type, ref byte* buffer) => *buffer++;
}