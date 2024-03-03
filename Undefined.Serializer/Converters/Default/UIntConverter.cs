using System.Runtime.CompilerServices;

namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class UIntConverter : PrimitiveCompressibleConverter<uint>
{
    private const int F_SIZE = 4;

    protected override void Serialize(uint o, ref byte* buffer)
    {
        Span<byte> data = stackalloc byte[F_SIZE];
        Unsafe.As<byte, uint>(ref data[0]) = o;
        var k = 1;
        for (var i = 0; i < F_SIZE; i++)
        {
            if (data[i] == 0)
                *buffer = (byte)(*buffer | (1 << i));
            else if (data[i] == 255)
                *buffer = (byte)(*buffer | (1 << (i + F_SIZE)));
            else
            {
                *(buffer + k) = data[i];
                k++;
            }
        }

        buffer += k;
    }

    protected override uint Deserialize(Type type, ref byte* buffer)
    {
        var bitB = *buffer;

        Span<byte> bytes = stackalloc byte[F_SIZE];
        var k = 1;
        for (var i = 0; i < F_SIZE; i++)
        {
            if (((bitB >> i) & 1) != 0)
                bytes[i] = 0;
            else if (((bitB >> (i + F_SIZE)) & 1) != 0)
                bytes[i] = 255;
            else
            {
                bytes[i] = *(buffer + k);
                k++;
            }
        }

        buffer += k;
        return Unsafe.ReadUnaligned<uint>(ref bytes[0]);
    }


    protected override int GetSize(uint value)
    {
        Span<byte> span = stackalloc byte[F_SIZE];
        Unsafe.As<byte, uint>(ref span[0]) = value;
        var count = 1;
        for (var i = 0; i < F_SIZE; i++)
            if (span[i] != 0 && span[i] != 255)
                count++;
        return count;
    }
}