using System;
using System.Net;

namespace RUDPSharp
{
    public sealed class PendingPacket {
            public PacketType PacketType { get; private set; }
            public Channel Channel { get; private set; }

            public EndPoint RemoteEndPoint { get; private set;}

            public int Sequence { get; private set; }

            public byte [] Data { get; private set; }

            public long Sent { get; set; }
            public short Attempts { get; set; }

            public static PendingPacket FromPacket (EndPoint endPoint, Packet packet, bool incoming = true)
            {
                return new PendingPacket () {
                    RemoteEndPoint = endPoint,
                    Sequence = packet.Sequence,
                    Channel = packet.Channel,
                    PacketType = packet.PacketType,
                    Data = incoming ? packet.Payload.ToArray () : packet.Data,
                    Sent = DateTime.Now.Ticks,
                };
            }
        }
}