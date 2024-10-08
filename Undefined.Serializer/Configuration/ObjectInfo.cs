namespace Undefined.Serializer.Configuration;

[Flags]
public enum ObjectInfo : byte
{
    None = 1 << 0,
    Compressed = 1 << 1,
    NullObject = 1 << 2
}