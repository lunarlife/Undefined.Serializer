using System.Runtime.CompilerServices;

namespace Undefined.Serializer.Converters.Default;

public sealed unsafe class ULongConverter : PrimitiveCompressibleConverter<ulong>
{
    private const int F_SIZE = sizeof(ulong);


    protected override void Serialize(ulong o, ref byte* buffer)
    {
        Span<byte> data = stackalloc byte[F_SIZE];
        Unsafe.As<byte, ulong>(ref data[0]) = o;
        var k = 1;
        for (var i = 0; i < F_SIZE; i++)
            if (data[i] == 0)
                *buffer = (byte)(*buffer | (1 << i));
            else
            {
                *(buffer + k) = data[i];
                k++;
            }

        buffer += k;
    }

    protected override ulong Deserialize(Type type, ref byte* buffer)
    {
        var bitB = *buffer;

        Span<byte> bytes = stackalloc byte[F_SIZE];
        var k = 1;
        for (var i = 0; i < F_SIZE; i++)
            if (((bitB >> i) & 1) != 0)
                bytes[i] = 0;
            else
            {
                bytes[i] = *(buffer + k);
                k++;
            }

        buffer += k;
        return Unsafe.ReadUnaligned<ulong>(ref bytes[0]);
    }

    protected override int GetSize(ulong value)
    {
        Span<byte> span = stackalloc byte[F_SIZE];
        Unsafe.As<byte, ulong>(ref span[0]) = value;
        var count = 1;
        for (var i = 0; i < F_SIZE; i++)
            if (span[i] != 0)
                count++;
        return count;
    }
}