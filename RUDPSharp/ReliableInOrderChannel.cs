using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;

namespace RUDPSharp
{
    public class ReliableInOrderChannel : ReliableChannel
    {
        PendingPacketCache pendingPacketCache = new PendingPacketCache ();
        public ReliableInOrderChannel(int maxBufferSize = 100) : base(maxBufferSize)
        {

        }

        protected override int CheckSequence(int packet, int sequence)
        {
            if (base.CheckSequence (packet, sequence) == 1)
                return -1;
            if (packet != sequence + 1)
                return 1;
            return base.CheckSequence (packet, sequence);
        }

        protected override bool QueueOrDiscardPendingPackages(EndPoint endPoint, PendingPacket packet)
        {
            if (!base.QueueOrDiscardPendingPackages (endPoint, packet)) {
                pendingPacketCache.TryCachePacket (packet);
                return false;
            }
            return true;
        }

        public override PendingPacket QueueIncomingPacket(EndPoint endPoint, Packet packet)
        {
            return base.QueueIncomingPacket(endPoint, packet);
        }

        public override bool TryGetNextIncomingPacket(out PendingPacket packet)
        {
            if (Incoming.TryPeek (out packet) || pendingPacketCache.HasPacketsToDeliver) {
                if (pendingPacketCache.TryGetPacketToDeliver (packet?.Sequence ?? -1, out packet)) {
                    return true;
                }
            }
            return base.TryGetNextIncomingPacket(out packet);
        }
    }
}
