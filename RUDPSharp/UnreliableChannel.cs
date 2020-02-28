using System;
using System.Collections.Generic;

namespace RUDPSharp
{
    public class UnreliableChannel {

        public class PendingPacket {
            public PacketType PacketType { get; private set; }
            public Channel Channel { get; private set; }

            public int Sequence { get; private set; }

            public byte [] Data { get; private set; }

            public static PendingPacket FromPacket (Packet packet)
            {
                return new PendingPacket () {
                    Sequence = packet.Sequence,
                    Channel = packet.Channel,
                    PacketType = packet.PacketType,
                    Data = packet.Payload.ToArray (),
                };
            }
        }
        Queue<PendingPacket> outgoing;
        Queue<PendingPacket> incoming;

        public UnreliableChannel (int maxBufferSize = 100)
        {
            outgoing = new Queue<PendingPacket> (maxBufferSize);
            incoming = new Queue<PendingPacket> (maxBufferSize);
        }
        public bool TryGetNextOutgoingPacket (out PendingPacket packet)
        {
            packet = null;
            if (outgoing.Count == 0)
                return false;
            packet = outgoing.Dequeue ();
            return true;
        }

        public bool TryGetNextIncomingPacket (out PendingPacket packet)
        {
            packet = null;
            if (incoming.Count == 0)
                return false;
            packet = incoming.Dequeue ();
            return true;
        }
        public virtual void QueueOutgoingPacket (Packet packet)
        {
            outgoing.Enqueue (PendingPacket.FromPacket (packet));
        }

        public virtual void QueueIncomingPacket (Packet packet)
        {
            incoming.Enqueue (PendingPacket.FromPacket (packet));
        }
    }
}
