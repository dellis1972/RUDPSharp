using System;

namespace RUDPSharp
{
    public ref struct Packet {
        byte [] rawData;
        Span<byte> span;

        public Packet (byte[] data)
        {
            rawData = data;
            span = new Span<byte>(rawData);
            var header = DecodeHeader (rawData[0]);
            PacketType = header.type;
            Channel = header.channel;
        }

        public Packet (PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            byte header = EncodeHeader (packetType, channel);
            rawData = new byte[payload.Length + 1];
            rawData[0] = header;
            for (int i=0; i< payload.Length; i++) 
                rawData[i-1] = payload[i];
            span = new Span<byte> (rawData);
            PacketType = packetType;
            Channel = channel;
        }

        public byte Header { get { return span[0]; }}

        public PacketType PacketType { get; private set; }

        public Channel Channel { get; private set; }

        public ReadOnlySpan<byte> Payload { get { return span.Slice (1); }}

        public byte[] Data { get { return rawData; }}

        static byte EncodeHeader(PacketType type, Channel channel)
        {   
            byte t = (byte)type;
            byte t1 = (byte)channel;
            byte shift = 4;
            byte header = (byte)(t1 << shift); 
            header = (byte)(header | t);
            return header;
        }

        static (PacketType type, Channel channel) DecodeHeader (byte header)
        {
            return (type : (PacketType)(header & 0x1F), channel : (Channel)(header >> 4));
        }

    }
}