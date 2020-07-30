using System;

namespace RUDPSharp
{
    /*
    * Packet Structure
    *
    * byte header   PacketType|Channel|FragmetedBit
    * byte sequence
    * byte sequence
    * byte+ payload
    */
    public ref struct Packet {
        const int  HEADER_OFFSET = 0;
        const int  SEQUENCE_OFFSET = 1;
        const int  PAYLOAD_OFFSET = 3;
        byte [] rawData;
        Span<byte> span;

        public Packet (byte[] data, int length)
        {
            rawData = data;
            span = new Span<byte>(rawData, 0, length);

            var header = DecodeHeader (rawData[0]);
            PacketType = header.type;
            Channel = header.channel;
            Fragmented = header.fragmented;
        }

        public Packet (PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            byte header = EncodeHeader (packetType, channel);
            rawData = new byte[payload.Length + PAYLOAD_OFFSET];
            rawData[HEADER_OFFSET] = header;
            span = new Span<byte> (rawData);
            payload.TryCopyTo (span.Slice (PAYLOAD_OFFSET));
            PacketType = packetType;
            Channel = channel;
            Fragmented = false;
            Sequence = 0;
        }

        public Packet (PacketType packetType, Channel channel, ushort sequence, ReadOnlySpan <byte> payload)
            : this (packetType, channel, payload)
        {
            Sequence = sequence;
        }

        public byte Header { get { return span[HEADER_OFFSET]; }}

        public PacketType PacketType { get; private set; }

        public Channel Channel { get; private set; }

        public bool Fragmented { get; private set; }

        public ushort Sequence {
            get { return DecodeSequence (); }
            set { EncodeSequence (value);}
        }

        public ReadOnlySpan<byte> Payload { get { return span.Slice (PAYLOAD_OFFSET); }}

        public byte[] Data { get { return rawData; }}

        static byte EncodeHeader(PacketType type, Channel channel, bool fragmented = false)
        {   
            // Header Format
            // 0b00000000
            //   FCCTTTTT
            // C = Channel 2 bits
            // T = Packet TYpe 5 bits
            // F = Fragmented bit
            byte t = (byte)type;
            byte t1 = (byte)channel;
            byte header = (byte)(t1 << 5);
            byte f = (byte)(fragmented ? 1 : 0 << 7); 
            header = (byte)(f | header | t);
            return header;
        }

        static (PacketType type, Channel channel, bool fragmented) DecodeHeader (byte header)
        {
            return (type : (PacketType)(header & 0x1F), channel : (Channel)((header & 0x60) >> 5), fragmented: (bool)(((header & 0x80) >> 7) == 1));
        }

        void EncodeSequence(ushort sequence)
        {
            WriteLittleEndian (rawData, SEQUENCE_OFFSET, (short)sequence);
        }

        ushort DecodeSequence ()
        {
            return BitConverter.ToUInt16 (rawData, SEQUENCE_OFFSET);
        }

        public static void WriteLittleEndian(byte[] buffer, int offset, short data)
        {
#if BIGENDIAN
            buffer[offset + 1] = (byte)(data);
            buffer[offset    ] = (byte)(data >> 8);
#else
            buffer[offset] = (byte)(data);
            buffer[offset + 1] = (byte)(data >> 8);
#endif
        }

    }
}