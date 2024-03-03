using System.Collections;

namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class DictionaryConverter : ICompressibleConverter<IDictionary>
{
    public bool Inheritance { get; } = true;
    public DataConverter Converter { get; init; }

    public void Serialize(IDictionary o, ref byte* buffer, bool compressed)
    {
        Converter.Serialize(o.Count, ref buffer, compressed);
        foreach (DictionaryEntry entry in o)
        {
            Converter.Serialize(entry.Key, ref buffer, compressed);
            Converter.Serialize(entry.Value, ref buffer, compressed);
        }
    }

    public IDictionary? Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        var count = Converter.Deserialize<int>(ref buffer);
        var dict = (IDictionary)RuntimeUtils.GetEmptyConstructor(type)!.Invoke(null);
        var types = type.GetGenericArguments();
        var type1 = types[0];
        var type2 = types[1];
        for (var i = 0; i < count; i++)
            dict.Add(Converter.Deserialize(type1, ref buffer)!,
                Converter.Deserialize(type2, ref buffer)!);
        return dict;
    }

    public int GetSize(IDictionary value, bool compressed)
    {
        var size = Converter.SizeOf(value.Count, compressed);
        foreach (DictionaryEntry entry in value)
        {
            size += Converter.SizeOf(entry.Key, compressed);
            size += Converter.SizeOf(entry.Value, compressed);
        }

        return size;
    }
}