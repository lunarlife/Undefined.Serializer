using System.Runtime.CompilerServices;

namespace Undefined.Serializer;

public static class ConvertUtils
{
    public static byte[] Combine(params byte[][] buffers)
    {
        var length = 0;
        foreach (var bytes in buffers) length += bytes.Length;
        var newArray = new byte[length];
        var index = 0;
        foreach (var bytes in buffers)
        {
            Unsafe.CopyBlock(ref newArray[index], ref bytes[0], (uint)bytes.Length);
            index += bytes.Length;
        }

        return newArray;
    }

    public static byte[] Combine(IEnumerable<byte[]> buffers)
    {
        var length = 0;
        foreach (var bytes in buffers) length += bytes.Length;
        var newArray = new byte[length];
        var index = 0;
        foreach (var bytes in buffers)
        {
            Unsafe.CopyBlock(ref newArray[index], ref bytes[0], (uint)bytes.Length);
            index += bytes.Length;
        }

        return newArray;
    }

    public static void Copy(byte[] destination, int index, params byte[][] buffers)
    {
        for (var i = 0; i < buffers.Length; i++)
        {
            var buffer = buffers[i];
            if (index + buffer.Length >= destination.Length) throw new IndexOutOfRangeException();
            Unsafe.CopyBlock(ref destination[index], ref buffer[0], (uint)buffer.Length);
            index += buffer.Length;
        }
    }

    public static void Copy(byte[] destination, int index, out int copyLength, params byte[][] buffers)
    {
        copyLength = 0;
        for (var i = 0; i < buffers.Length; i++)
        {
            var buffer = buffers[i];
            if (index + buffer.Length >= destination.Length) throw new IndexOutOfRangeException();
            Unsafe.CopyBlock(ref destination[index], ref buffer[0], (uint)buffer.Length);
            index += buffer.Length;
            copyLength += buffer.Length;
        }
    }
}