using System.Net.Sockets;
using Undefined.Serializer.Buffers;
using Undefined.Verifying;

namespace Undefined.Serializer;

public static class Extensions
{
    public static int Receive(this Socket socket, BufferWriter writer, int length)
    {
        if (length == 0) return 0;
        Verify.Positive(length);
        Verify.Argument(socket.Available >= length);
        if (writer.Left < length)
        {
            Verify.Argument(writer.Buffer.IsResizable, $"Buffer has no space for receive {length} bytes.");
            writer.Buffer.Expand(length - writer.Left + 1);
        }

        var received = socket.Receive(writer.Buffer.GetBuffer(), writer.Position, length, SocketFlags.None);
        writer.Position += received;
        return received;
    }

    public static int Receive(this Socket socket, BufferWriter writer) => socket.Receive(writer, socket.Available);
}