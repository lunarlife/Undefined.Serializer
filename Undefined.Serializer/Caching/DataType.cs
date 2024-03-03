using System.Reflection;
using System.Runtime.CompilerServices;
using Undefined.Serializer.Configuration;
using Undefined.Serializer.Converters;

namespace Undefined.Serializer.Caching;

internal class DataType
{
    public Type Type { get; }
    public DataConverter Converter { get; }
    public IConverterBase? TypeConverter { get; }
    private readonly Func<object>? _instanceFunc;
    private DataField[] _fields;
    private int _dataFieldsCount;

    public DataType(Type type, DataConverter converter, IConverterBase? typeConverter)
    {
        Type = type;
        Converter = converter;
        TypeConverter = typeConverter;
        if (typeConverter is null)
        {
            _instanceFunc = RuntimeUtils.IL_CreateInstanceMethodAsObject(type);
        }
        else Type = typeConverter.ImplementationType;

        _fields = typeConverter is null ? FindFields(type) : Array.Empty<DataField>();
    }


    public unsafe void Serialize(object? o, ref byte* buffer, bool compressed,
        SwitcherSettings? switcherSettings = null, ConverterUsing converterUsing = ConverterUsing.All)
    {
        Converter.WriteObjectInfo(buffer, compressed, o == null);
        buffer++;
        if (o == null) return;
        if (PrepareConverter(ref converterUsing) is { } converter)
        {
            switch (converter)
            {
                case ICompressibleConverter compressibleBase:
                {
                    compressibleBase.Serialize(o, ref buffer, compressed);
                    break;
                }
                case IConverter conv:
                {
                    conv.Serialize(o, ref buffer);
                    break;
                }
                default:
                    return;
            }
        }
        else
        {
            var dataCount = 0;
            byte* dataCountPtr = null;
            foreach (var field in _fields)
            {
                if (!field.IsMustSerialize(o, switcherSettings))
                    continue;

                var value = field.GetValue(o);

                if (field.DataFieldId is { } id)
                {
                    if (dataCount == 0)
                    {
                        dataCountPtr = buffer;
                        buffer++;
                    }

                    *buffer = (byte)id;
                    dataCount++;
                    buffer++;
                }

                var dataType = field.FieldCachedType;

                dataType.Serialize(value, ref buffer, compressed, switcherSettings,
                    converterUsing);
            }

            if (_dataFieldsCount != 0)
                *dataCountPtr = (byte)dataCount;
        }
    }

    private DataField[] FindFields(Type type)
    {
        var defaultFields = new List<DataField>();
        var dataFields = new List<DataField>();
        var baseType = type;
        while (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
        {
            var att = baseType.GetCustomAttribute<DataConvertParamAttribute>()?.Types;
            if (att is null || att.Value.HasFlag(IncludeDataType.Property))
            {
                var properties =
                    type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
                for (var i = 0; i < properties.Length; i++)
                {
                    var prop = properties[i];
                    if (RuntimeUtils.TryGetAttribute<ExcludeDataAttribute>(prop, out _)) continue;
                    int? switcher = null;
                    if (prop.GetCustomAttribute<DataSwitchAttribute>(true) is { } s) switcher = s.Id;
                    if (prop.GetBackingField() is not { } backingField)
                        continue;
                    var cachedField = new DataField(prop.Name, Converter.GetDataType(backingField.FieldType), switcher,
                        backingField, dataFields.Count);
                    if (cachedField.IsDataField)
                        dataFields.Add(cachedField);
                    else defaultFields.Add(cachedField);
                }
            }

            if (att is null || att.Value.HasFlag(IncludeDataType.Field))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (RuntimeUtils.TryGetAttribute<ExcludeDataAttribute>(f, out _)) continue;
                    if (RuntimeUtils.TryGetAttribute<CompilerGeneratedAttribute>(f, out _))
                        continue;
                    int? switcher = null;
                    if (f.GetCustomAttribute<DataSwitchAttribute>(true) is { } s) switcher = s.Id;
                    var cachedField = new DataField(f.Name, Converter.GetDataType(f.FieldType), switcher, f,
                        dataFields.Count);
                    if (cachedField.IsDataField) dataFields.Add(cachedField);
                    else defaultFields.Add(cachedField);
                }
            }

            baseType = baseType.BaseType;
        }

        _dataFieldsCount = dataFields.Count;
        defaultFields.AddRange(dataFields);
        return defaultFields.ToArray();
    }

    public int GetSize(object? o, bool compressed)
    {
        if (o is null)
            return DataConverter.OBJECT_INFO_LENGTH;
        var size = 0;
        CalculateSize(o, compressed, ref size);
        if (_dataFieldsCount != 0) size++;
        return size + DataConverter.OBJECT_INFO_LENGTH;
    }

    private void CalculateSize(object o, bool compressed, ref int size)
    {
        if (TypeConverter != null)
            size += TypeConverter is IConverter converter
                ? converter.GetSize(o)
                : ((ICompressibleConverter)TypeConverter).GetSize(o, compressed);
        else
            foreach (var field in _fields)
                size += field.GetSize(o, compressed);
    }

    private IConverterBase? PrepareConverter(ref ConverterUsing converterUsing)
    {
        var converter = converterUsing is ConverterUsing.ExcludeCurrent or ConverterUsing.ExcludeAll
            ? null
            : TypeConverter;
        converterUsing = converterUsing switch
        {
            ConverterUsing.ExcludeCurrent => ConverterUsing.All,
            ConverterUsing.OnlyCurrent => ConverterUsing.ExcludeAll,
            _ => converterUsing
        };
        return converter;
    }

    public unsafe object? Deserialize(Type type, ref byte* buffer, SwitcherSettings? switcherSettings = null,
        ConverterUsing converterUsing = ConverterUsing.All, ConverterUsing invokeEvents = ConverterUsing.All)

    {
        GetInfo(*buffer, out var compressed, out var isNull);
        buffer++;
        if (isNull) return null;
        if (PrepareConverter(ref converterUsing) is { } converter)
        {
            switch (converter)
            {
                case ICompressibleConverter compressibleBase:
                {
                    var deserialize =
                        compressibleBase.Deserialize(type, ref buffer, compressed);
                    return deserialize;
                }
                case IConverter converterBase:
                {
                    var deserialize = converterBase.Deserialize(type, ref buffer);
                    return deserialize;
                }
            }
        }

        var obj = _instanceFunc!();
        DeserializeInjectInternal(ref obj, ref buffer, switcherSettings, converterUsing,
            invokeEvents);
        return obj;
    }

    private unsafe void DeserializeInjectInternal(ref object o, ref byte* buffer,
        SwitcherSettings? switcherSettings, ConverterUsing converterUsing, ConverterUsing invokeEvents)
    {
        for (var i = 0; i < _fields.Length - _dataFieldsCount; i++)
        {
            var field = _fields[i];
            if (switcherSettings is { } switcher)
            {
                if (switcher.ExcludeNoSwitchers && field.Switcher == null)
                    continue;
                if (field.Switcher != switcher.Switcher) continue;
            }

            var deserialize = field.FieldCachedType.Deserialize(field.FieldType, ref buffer,
                switcherSettings, converterUsing, invokeEvents);
            field.SetValue(ref o, deserialize);
        }

        if (_dataFieldsCount == 0) return;
        var count = *buffer++;
        for (var i = 0; i < count; i++)
        {
            var dataId = *buffer++;
            var field = _fields[_fields.Length - _dataFieldsCount + dataId];

            var deserialize = field.FieldCachedType.Deserialize(field.FieldType, ref buffer,
                switcherSettings, converterUsing, invokeEvents);
            field.SetValue(ref o, deserialize);
        }
    }

    private void GetInfo(byte b, out bool compressed, out bool isNull)
    {
        compressed = (b & (1 << 1)) != 0;
        isNull = (b & (1 << 2)) != 0;
    }
}