namespace Serializers
{
    using System;
    using System.Buffers;
    using UdpToolkit.Serialization;

    public class MessagePackSerializer : ISerializer
    {
        public void Serialize<T>(IBufferWriter<byte> buffer, T item)
        {
            MessagePack.MessagePackSerializer.Serialize(buffer, item);
        }

        public void SerializeUnmanaged<T>(IBufferWriter<byte> buffer, T item)
            where T : unmanaged
        {
            throw new NotSupportedException();
        }

        public T Deserialize<T>(ReadOnlySpan<byte> buffer, T item)
        {
            // MessagePack does not support deserialization to an existing object
            return MessagePack.MessagePackSerializer.Deserialize<T>(buffer.ToArray());
        }

        public T DeserializeUnmanaged<T>(ReadOnlySpan<byte> buffer)
            where T : unmanaged
        {
            throw new NotSupportedException();
        }
    }
}