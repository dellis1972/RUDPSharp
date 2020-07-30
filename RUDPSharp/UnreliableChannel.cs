using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace RUDPSharp
{
    public class UnreliableChannel {

        public class PendingPacket {
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
        ConcurrentQueue<PendingPacket> outgoing;
        ConcurrentQueue<PendingPacket> incoming;

        protected ConcurrentQueue<PendingPacket> Outgoing => outgoing;
        protected ConcurrentQueue<PendingPacket> Incoming => incoming;

        public UnreliableChannel (int maxBufferSize = 100)
        {
            outgoing = new ConcurrentQueue<PendingPacket> ();
            incoming = new ConcurrentQueue<PendingPacket> ();
        }
        public bool TryGetNextOutgoingPacket (out PendingPacket packet)
        {
            packet = null;
            if (outgoing.Count == 0)
                return false;
            return outgoing.TryDequeue (out packet);
        }

        public bool TryGetNextIncomingPacket (out PendingPacket packet)
        {
            packet = null;
            if (incoming.Count == 0)
                return false;
            return incoming.TryDequeue (out packet);
        }
        public virtual PendingPacket QueueOutgoingPacket (EndPoint endPoint, Packet packet)
        {
            var pendingPacket = PendingPacket.FromPacket (endPoint, packet, incoming: false);
            outgoing.Enqueue (pendingPacket);
            return pendingPacket;
        }

        public virtual PendingPacket QueueIncomingPacket (EndPoint endPoint, Packet packet)
        {
            var pendingPacket = PendingPacket.FromPacket (endPoint, packet);
            incoming.Enqueue (pendingPacket);
            return pendingPacket;
        }

        public virtual IEnumerable<PendingPacket> GetPendingOutgoingPackets ()
        {
            while (TryGetNextOutgoingPacket (out PendingPacket pendingPacket)) {
                yield return pendingPacket;
            }
        }

        public virtual IEnumerable<PendingPacket> GetPendingIncomingPackets ()
        {
            while (TryGetNextIncomingPacket (out PendingPacket pendingPacket)) {
                yield return pendingPacket;
            }
        }
    }
}
