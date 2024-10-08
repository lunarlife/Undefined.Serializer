using Undefined.Serializer.Exceptions;

namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class TypeConverter : ICompressibleConverter<Type>
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Dictionary<string, Type>> _types = new();

    public DataConverter Converter { get; init; }

    public void Serialize(Type o, ref byte* buffer, bool compressed)
    {
        Converter.Serialize(o.Assembly.GetName().Name!, ref buffer, compressed);
        Converter.Serialize(o.Name, ref buffer, compressed);
    }

    public Type? Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        var assembly = Converter.Deserialize<string>(ref buffer)!;
        var typeName = Converter.Deserialize<string>(ref buffer)!;
        return GetType(assembly, typeName);
    }

    public int GetSize(Type value, bool compressed) => Converter.SizeOf(value.Assembly.GetName().Name!, compressed) +
                                                       Converter.SizeOf(value.Name, compressed);

    private Type GetType(string @namespace, string name)
    {
        lock (_lock)
        {
            if (!_types.TryGetValue(@namespace, out var dict))
            {
                dict = new Dictionary<string, Type>();
                _types.Add(@namespace, dict);
            }

            if (dict.TryGetValue(name, out var type)) return type;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetName().Name != @namespace) continue;
                foreach (var t in assembly.GetTypes())
                    if (t.Name == name)
                        type = t;
                if (type is null) throw new DeserializeException("type not found");
                dict.Add(name, type!);
                break;
            }

            if (type is null) throw new DeserializeException("type not found");
            return type;
        }
    }
}