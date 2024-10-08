using System.Reflection;
using Undefined.Serializer.Configuration;
using Undefined.Serializer.Exceptions;

namespace Undefined.Serializer.Caching;

internal class DataField
{
    private static readonly Type FieldDataType = typeof(Data<>);
    private static readonly Type NullableDataType = typeof(Nullable<>);
    private readonly FieldInfo _fieldInfo;
    private readonly PropertyInfo? _isMustSerializeProperty;
    private readonly FieldSetter _setter;
    private readonly FieldInfo? _valueField;
    private readonly FieldSetter? _valueFieldSetter;
    public DataType FieldCachedType { get; }
    public int? Switcher { get; }
    public Type FieldType { get; }
    public bool IsNullable { get; }
    public bool IsDataField => DataFieldId is not null;
    public int? DataFieldId { get; }

    public string Name { get; }

    public DataField(string name, DataConverter converter, int? switcher, FieldInfo fieldInfo, int dataFieldId)
    {
        _fieldInfo = fieldInfo;
        Name = name;
        Switcher = switcher;
        var t = fieldInfo.FieldType;
        if (t.IsGenericType)
        {
            var definition = t.GetGenericTypeDefinition();
            if (definition == NullableDataType)
            {
                var unType = Nullable.GetUnderlyingType(t)!;
                if (unType.IsGenericType && unType.GetGenericTypeDefinition() == FieldDataType)
                    throw new FieldException("Field with nullable data not allowed.");
                FieldType = unType;
                IsNullable = true;
            }
            else if (definition == FieldDataType)
            {
                DataFieldId = dataFieldId;
                var underType = t.GetGenericArguments()[0];
                if (underType.IsGenericType && Nullable.GetUnderlyingType(underType) is { } underlyingType)
                {
                    IsNullable = true;
                    FieldType = underlyingType;
                }
                else FieldType = underType;

                _isMustSerializeProperty =
                    t.GetProperty("IsMustSerialize", BindingFlags.Instance | BindingFlags.NonPublic);
                _valueField = t.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
                _valueFieldSetter = RuntimeUtils.IL_CreateFieldSetter(_valueField!);
            }
            else FieldType = t;
        }
        else FieldType = t;

        FieldCachedType = converter.GetDataType(FieldType);
        _setter = RuntimeUtils.IL_CreateFieldSetter(fieldInfo);
    }

    public bool IsMustSerialize(object o, SwitcherSettings? switcherSettings)
    {
        if (switcherSettings is { } switcher)
        {
            if (switcher.ExcludeNoSwitchers && Switcher == null)
                return false;
            if (Switcher != switcher.Switcher) return false;
        }

        return !IsDataField ||
               (bool)_isMustSerializeProperty!.GetValue(_fieldInfo.GetValue(o))!;
    }

    public int GetSize(object o, bool compressed)
    {
        var size = FieldCachedType.GetSize(GetValue(o), compressed);
        if (DataFieldId is not null) size++;
        return size;
    }

    public object? GetValue(object obj) => IsDataField
        ? _valueField!.GetValue(_fieldInfo.GetValue(obj)!)
        : _fieldInfo.GetValue(obj);

    public void SetValue(ref object obj, object? value)
    {
        if (!IsDataField)
        {
            _setter(ref obj, value);
            return;
        }

        var data = _fieldInfo.GetValue(obj)!;
        if (!(bool)_isMustSerializeProperty!.GetValue(data)!) return;
        _valueFieldSetter!(ref data, value);
        _setter(ref obj, data);
    }
}