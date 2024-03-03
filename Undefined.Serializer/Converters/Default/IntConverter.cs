using System.Runtime.CompilerServices;

namespace Undefined.Serializer.Converters.Default;

public sealed class IntConverter : PrimitiveCompressibleConverter<int>
{
    private const int F_SIZE = 4;
    protected override unsafe int Deserialize(Type type, ref byte* buffer)
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
        return Unsafe.ReadUnaligned<int>(ref bytes[0]);
    }


    protected override unsafe void Serialize(int o, ref byte* buffer)
    {
        Span<byte> data = stackalloc byte[F_SIZE];
        Unsafe.As<byte, int>(ref data[0]) = o;
        var k = 1;
        for (var i = 0; i < F_SIZE; i++)
        {
            switch (data[i])
            {
                case 0:
                    *buffer = (byte)(*buffer | (1 << i));
                    break;
                case 255:
                    *buffer = (byte)(*buffer | (1 << (i + F_SIZE)));
                    break;
                default:
                {
                    *(buffer + k) = data[i];
                    k++;
                    break;
                }
            }
        }

        buffer += k;
    }

    protected override int GetSize(int value)
    {
        Span<byte> span = stackalloc byte[F_SIZE];
        Unsafe.As<byte, int>(ref span[0]) = value;
        var count = 1;
        for (var i = 0; i < F_SIZE; i++)
            if (span[i] != 0 && span[i] != 255)
                count++;
        return count;
    }
}