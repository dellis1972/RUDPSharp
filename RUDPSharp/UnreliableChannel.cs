using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace RUDPSharp
{
    public class UnreliableChannel {

        ConcurrentQueue<PendingPacket> outgoing;
        ConcurrentQueue<PendingPacket> incoming;

        protected ConcurrentQueue<PendingPacket> Outgoing => outgoing;
        protected ConcurrentQueue<PendingPacket> Incoming => incoming;

        protected int MaxBufferSize = 1024;

        public UnreliableChannel (int maxBufferSize = 1024)
        {
            outgoing = new ConcurrentQueue<PendingPacket> ();
            incoming = new ConcurrentQueue<PendingPacket> ();
            MaxBufferSize = maxBufferSize;
        }
        public bool TryGetNextOutgoingPacket (out PendingPacket packet)
        {
            packet = null;
            if (outgoing.Count == 0)
                return false;
            return outgoing.TryDequeue (out packet);
        }

        public virtual bool TryGetNextIncomingPacket (out PendingPacket packet)
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
            if (packet.Fragmented) {
                //TODO put the fragment in a bucket for processing when we get the rest of it.
            }
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
