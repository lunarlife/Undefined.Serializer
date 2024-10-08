namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class ArrayConverter : ICompressibleConverter<Array>
{
    public DataConverter Converter { get; init; }

    public void Serialize(Array o, ref byte* buffer, bool compressed)
    {
        var arrayType = o.GetType().GetElementType();
        var rank = o.Rank;
        if (arrayType is null) throw new ArgumentException("Array has unknown type.");
        var lengths = new int[rank];
        for (var i = 0; i < rank; i++) Converter.Serialize(lengths[i] = o.GetLength(i), ref buffer, compressed);

        var indices = GetIndicesArray(rank);
        if (arrayType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(arrayType);
            while (IterateArray(indices, lengths)) dataType.Serialize(o.GetValue(indices), ref buffer, compressed);
            return;
        }

        while (IterateArray(indices, lengths))
        {
            var obj = o.GetValue(indices);
            var dataType = Converter.GetDataType(obj.GetType());
            dataType.Serialize(obj, ref buffer, compressed);
        }
    }

    public Array? Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        var dimensions = type.GetArrayRank();
        var lengths = new int[dimensions];
        var totalLength = 0;
        for (var i = 0; i < dimensions; i++)
        {
            var arraySize = lengths[i] =
                Converter.Deserialize<int>(ref buffer);
            totalLength += arraySize;
        }

        var arrayType = type.GetElementType()!;
        var array = Array.CreateInstance(arrayType, lengths);
        if (dimensions == 0 || totalLength == 0) return array;
        var indices = GetIndicesArray(dimensions);
        if (arrayType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(arrayType);
            while (IterateArray(indices, lengths))
                array.SetValue(dataType.Deserialize(arrayType, ref buffer),
                    indices);
        }
        else
            while (IterateArray(indices, lengths))
                array.SetValue(Converter.Deserialize(arrayType, ref buffer),
                    indices);

        return array;
    }


    public int GetSize(Array array, bool compressed)
    {
        var rank = array.Rank;
        var size = 0;
        var length = 0;
        for (var i = 0; i < rank; i++)
        {
            var l = array.GetLength(i);
            size += Converter.SizeOf(l, compressed);
            if (length == 0)
                length = l;
            else length *= l;
        }

        var arrayType = array.GetType().GetElementType();
        if (arrayType is null) throw new ArgumentException("Array has unknown type.");
        if (arrayType is { IsAbstract: false, IsInterface: false })
        {
            var dataType = Converter.GetDataType(arrayType);
            if (compressed)
                foreach (var obj in array)
                    size += dataType.GetSize(obj, compressed);
            else size += length > 0 ? dataType.GetSize(array.GetValue(new int[array.Rank]), false) * length : 0;
        }
        else
            foreach (var obj in array)
            {
                var dataType = Converter.GetDataType(obj.GetType());
                size += dataType.GetSize(obj, compressed);
            }

        return size;
    }

    private static int[] GetIndicesArray(int dimensions)
    {
        var array = new int[dimensions];
        array[0] = -1;
        return array;
    }


    private bool IterateArray(int[] indices, int[] lengths)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            var len = lengths[i];
            var index = indices[i];
            if (index >= len - 1)
            {
                if (i >= indices.Length) return false;

                indices[i] = 0;
            }
            else
            {
                indices[i]++;
                return true;
            }
        }

        return false;
    }
}