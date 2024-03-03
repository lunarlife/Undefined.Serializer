namespace Undefined.Serializer;

[Flags]
public enum ReflectionType
{
    Static = 1 << 0,
    Instance = 1 << 1
}