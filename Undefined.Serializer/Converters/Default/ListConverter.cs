using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Undefined.Serializer.Exceptions;

namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class ListConverter : ICompressibleConverter<IList>
{
    public bool Inheritance => true;
    public DataConverter Converter { get; init; }


    public void Serialize(IList o, ref byte* buffer, bool compressed)
    {
        Converter.Serialize(o.Count, ref buffer, compressed);
        var type = o.GetType();
        if (type.IsGenericType && type.GetGenericArguments()[0] == typeof(byte))
        {
            fixed (byte* bytes =
                       (byte[])RuntimeUtils.GetListUnderlyingArray(o)) Unsafe.CopyBlock(buffer, bytes, (uint)o.Count);
            buffer += o.Count;
            return;
        }

        var arguments = type.GetGenericArguments();
        if (arguments.Length == 0)
            throw new ConverterException("List has unknown type.");
        var listType = arguments[0];
        if (listType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(listType);
            foreach (var obj in o) dataType.Serialize(obj, ref buffer, compressed);

            return;
        }

        foreach (var obj in o)
        {
            var dataType = Converter.GetDataType(obj.GetType());
            dataType.Serialize(obj, ref buffer, compressed);
        }
    }


    public IList? Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        var arrayType = type.GetGenericArguments()[0];
        if (arrayType == null) throw new DeserializeException($"{type.Name} is not array");
        var listSize = Converter.Deserialize<int>(ref buffer);
        if (arrayType == typeof(byte))
        {
            var array = new byte[listSize];
            fixed (byte* b = array) Unsafe.CopyBlock(b, buffer, (uint)listSize);
            var deserialize = new List<byte>(array);
            buffer += listSize;
            return deserialize;
        }

        var list = (IList)Activator.CreateInstance(type, listSize)!;
        if (arrayType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(arrayType);
            for (var i = 0; i < listSize; i++)
            {
                list.Add(dataType.Deserialize(arrayType, ref buffer));
            }
        }
        else
            for (var i = 0; i < listSize; i++)
            {
                list.Add(Converter.Deserialize(arrayType, ref buffer));
            }

        return list;
    }

    public int GetSize(IList value, bool compressed)
    {
        var size = Converter.SizeOf(value.Count, compressed);
        var arguments = value.GetType().GetGenericArguments();
        if (arguments.Length == 0)
            throw new SerializeException("List has unknown type.");
        var arrayType = arguments[0];
        if (arrayType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(arrayType);
            foreach (var obj in value)
                size += dataType.GetSize(obj, compressed);
        }
        else
            foreach (var obj in value)
            {
                var dataType = Converter.GetDataType(obj.GetType());
                size += dataType.GetSize(obj, compressed);
            }

        return size;
    }
}