using System.Runtime.CompilerServices;
using System.Text;

namespace Undefined.Serializer.Converters.Default;

public sealed class StringConverter : ICompressibleConverter<string>
{
    public const int MAX_STRING_LENGTH = 10000;
    public DataConverter Converter { get; init; }


    public unsafe void Serialize(string o, ref byte* buffer, bool compressed)
    {
        if (compressed)
        {
            var encoding = Encoding.UTF8;
            var bytes = encoding.GetBytes(o);
            fixed (byte* b = bytes)
            {
                Unsafe.CopyBlock(buffer, b, (uint)bytes.Length);
            }

            buffer += bytes.Length;
        }
        else
        {
            var strLength = o.Length * 2;
            if (strLength != 0)
                fixed (void* ptr = o)
                {
                    Unsafe.CopyBlock(buffer, ptr, (uint)strLength);
                }

            buffer += strLength;
            *buffer++ = 0;
        }

        *buffer++ = 0;
    }

    public unsafe string? Deserialize(Type type, ref byte* buffer, bool compressed)
    {
        if (compressed)
        {
            var stringLength = 0;
            while (stringLength < MAX_STRING_LENGTH)
            {
                if (*(buffer + stringLength) != 0)
                {
                    stringLength++;
                    continue;
                }

                break;
            }


            var deserialize = Encoding.UTF8.GetString(buffer, stringLength);
            buffer += stringLength + 1;
            return deserialize;
        }

        if (*buffer == 0 && *(buffer + 1) == 0)
        {
            buffer += 2;
            return string.Empty;
        }

        var stringLen = 1;
        while (stringLen < MAX_STRING_LENGTH)
        {
            if (buffer[stringLen - 1] == 0 && buffer[stringLen] == 0)
                break;

            stringLen++;
        }

        if (stringLen == 1) return string.Empty;
        var length = stringLen;
        var str = new string((char*)buffer, 0, length / 2);
        buffer += stringLen + 2;
        return str;
    }

    public int GetSize(string value, bool compressed) =>
        compressed ? Encoding.UTF8.GetByteCount(value) + 1 : value.Length * 2 + 2;
}