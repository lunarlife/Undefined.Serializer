namespace Undefined.Serializer.Configuration;

[Flags]
public enum IncludeDataType
{
    Field = 1 << 0,
    Property = 1 << 1
}