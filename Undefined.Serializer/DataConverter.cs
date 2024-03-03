using System.Runtime.CompilerServices;
using Undefined.Serializer.Caching;
using Undefined.Serializer.Configuration;
using Undefined.Serializer.Converters;
using Undefined.Serializer.Converters.Default;
using Undefined.Serializer.Exceptions;

namespace Undefined.Serializer;

public sealed class DataConverter
{
    public const int OBJECT_INFO_LENGTH = 1;

    private readonly List<IConverterBase> _converters = [];
    private readonly Dictionary<Type, IConverterBase> _primitiveConverters = new();

    private readonly Dictionary<Type, DataType> _types = new();

    public byte[] Serialize(object? obj, bool compressed, SwitcherSettings? switcher = null,
        ConverterUsing converterUsing = ConverterUsing.All)
    {
        unsafe
        {
            if (obj is null)
                return FlushInfo(new byte[1], 0, compressed, true);
            var type = obj.GetType();
            var dataType = GetDataType(type);
            var size = dataType.GetSize(obj, compressed);
            if (obj is ISerializeHandler h) h.OnSerialize();
            var array = new byte[size];
            fixed (byte* b = array)
            {
                var buffer = b;
                dataType.Serialize(obj, ref buffer, compressed);
            }
            return array;
        }
    }

    public unsafe void Serialize(object? obj, ref byte* buffer, bool compressed)
    {
    }

    public unsafe object? Deserialize(Type type, ref byte* buffer,
        SwitcherSettings? switcher = null,
        ConverterUsing converterUsing = ConverterUsing.All,
        ConverterUsing invokeEvents = ConverterUsing.All)
    {
        var dataType = GetDataType(RuntimeUtils.GetNotNullableType(type));
        return dataType.Deserialize(type, ref buffer, switcher, converterUsing: converterUsing,
            invokeEvents: invokeEvents);
    }
    public unsafe T? Deserialize<T>(byte[] buffer, SwitcherSettings? switcher = null,
        ConverterUsing converterUsing = ConverterUsing.All,
        ConverterUsing invokeEvents = ConverterUsing.All)
    {
        fixed (byte* b = buffer)
        {
            var bytes = b;
            return (T?)Deserialize(typeof(T), ref bytes, switcher, converterUsing, invokeEvents);
        }
    }

    public unsafe T? Deserialize<T>(ref byte* buffer, SwitcherSettings? switcher = null,
        ConverterUsing converterUsing = ConverterUsing.All,
        ConverterUsing invokeEvents = ConverterUsing.All) =>
        (T?)Deserialize(typeof(T), ref buffer, switcher, converterUsing, invokeEvents);

    public DataConverter RegisterConverter<T>(bool replaceIfRegistered = false) where T : IConverterBase, new() =>
        RegisterConverter(new T
        {
            Converter = this
        }, replaceIfRegistered);

    public DataConverter RegisterConverter<T>(T converter, bool replaceIfRegistered = false) where T : IConverterBase
    {
        var type = converter.ImplementationType;
        if (type.IsPrimitive || type.IsArray || !converter.Inheritance)
        {
            if (_primitiveConverters.TryAdd(type, converter)) return this;
            if (replaceIfRegistered)
                _primitiveConverters[type] = converter;
            else
                throw new DataConverterException(
                    $"Converter for type {converter.ImplementationType.Name} already registered.");
        }
        else
        {
            foreach (var c in _converters)
                if (c.ImplementationType == type)
                    throw new DataConverterException(
                        $"Converter for type {converter.ImplementationType.Name} already registered.");
            _converters.Add(converter);
        }

        return this;
    }

    public IConverterBase? GetConverterForType(Type type)
    {
        type = GetPrimitiveType(type);
        if (_primitiveConverters.TryGetValue(type, out var converter)) return converter;

        foreach (var c in _converters)
        {
            if (!c.ImplementationType.IsAssignableFrom(type)) continue;
            return c;
        }

        return null;
    }

    private Type GetPrimitiveType(Type type)
    {
        if (type.IsArray) return typeof(Array);
        if (type.IsEnum) return typeof(Enum);
        return RuntimeUtils.GetNotNullableType(type);
    }

    internal DataType GetDataType(Type type)
    {
        type = GetPrimitiveType(type);
        if (!_types.TryGetValue(type, out var dataType)) dataType = CreateDataType(type);
        return dataType;
    }

    private DataType CreateDataType(Type type)
    {
        var converter = GetConverterForType(type);
        var dataType = new DataType(type, this, converter);
        _types.Add(dataType.Type, dataType);
        return dataType;
    }

    public unsafe void WriteObjectInfo(byte* buffer, bool compressed, bool isNull)
    {
        var info = ObjectInfo.None;
        if (compressed) info |= ObjectInfo.Compressed;
        if (isNull) info |= ObjectInfo.NullObject;
        *buffer = (byte)info;
    }

    public byte[] FlushInfo(byte[] buffer, int index, bool compressed, bool isNull)
    {
        var info = ObjectInfo.None;
        if (compressed) info |= ObjectInfo.Compressed;
        if (isNull) info |= ObjectInfo.NullObject;
        buffer[index] = (byte)info;
        return buffer;
    }

    public int SizeOf(object value, bool compressed) => GetDataType(value.GetType()).GetSize(value, compressed);
    public int SizeOf<T>(T value, bool compressed) => GetDataType(typeof(T)).GetSize(value, compressed);
    public int SizeOfDefault<T>(int count = 1) => GetDataType(typeof(T)).GetSize(default(T), false) * count;

    public int SizeOf<T>(bool compressed, IEnumerable<T> values)
    {
        var type = GetDataType(typeof(T));
        var size = 0;
        foreach (var value in values) size += type.GetSize(value, compressed);
        return size;
    }

    public int SizeOf<T>(bool compressed, params T[] values) => SizeOf(compressed, (IEnumerable<T>)values);

    public static DataConverter GetDefault()
    {
        var converter = new DataConverter();
        return converter.RegisterConverter<ArrayConverter>()
            .RegisterConverter<BoolConverter>()
            .RegisterConverter<StringConverter>()
            .RegisterConverter<DoubleConverter>()
            .RegisterConverter<ByteConverter>()
            .RegisterConverter<IntConverter>()
            .RegisterConverter<UIntConverter>()
            .RegisterConverter<FloatConverter>()
            .RegisterConverter<ShortConverter>()
            .RegisterConverter<UShortConverter>()
            .RegisterConverter<LongConverter>()
            .RegisterConverter<ULongConverter>()
            .RegisterConverter<EnumConverter>()
            .RegisterConverter<ListConverter>()
            .RegisterConverter<TypeConverter>()
            .RegisterConverter<DictionaryConverter>();
    }
}