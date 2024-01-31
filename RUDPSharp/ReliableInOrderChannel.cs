using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;

namespace RUDPSharp
{
    public class ReliableInOrderChannel : ReliableChannel
    {
        PacketAcknowledgement acknowledgement = new PacketAcknowledgement();
        SortedList<int, PendingPacket> pendingPackets = new SortedList<int, PendingPacket> ();

        public TimeSpan PacketExpire {get ;set; } = TimeSpan.FromMilliseconds(500);
        public ReliableInOrderChannel(int maxBufferSize = 100) : base(maxBufferSize)
        {

        }

        protected override int CheckSequence(int packet, int sequence)
        {
            if (packet != sequence + 1)
                return 1;
            return base.CheckSequence (packet, sequence);
        }

        protected override bool QueueOrDiscardPendingPackages(EndPoint endPoint, PendingPacket packet)
        {
            foreach (var pending in pendingPackets) {
                if (pending.Key < RemoteSequence) {
                    Incoming.Enqueue (pending.Value);
                }
            }
            if (!base.QueueOrDiscardPendingPackages (endPoint, packet)) {
                pendingPackets.Add (packet.Sequence, packet);
                return false;
            }
            return true;
        }

        public override PendingPacket QueueIncomingPacket (EndPoint endPoint, Packet packet)
        {
            PendingPacket pendingPacket = base.QueueIncomingPacket (endPoint, packet);
            foreach (var pending in pendingPackets) {
                if (!base.QueueOrDiscardPendingPackages (pending.Value.RemoteEndPoint, pending.Value)) {
                    break;
                }
                Incoming.Enqueue (pending.Value);
            }
            long now = DateTime.Now.Ticks;
            for (int i=pendingPackets.Count-1; i >= 0 ; i--) {
                pendingPacket = pendingPackets.Values[0];
                if (pendingPackets.Keys[i] < RemoteSequence || (now - pendingPacket.Sent > PacketExpire.Ticks)) {
                    pendingPackets.RemoveAt (i);
                    Debug.WriteLine ($"Dropping Packet from {endPoint}. Packet is too old {now - pendingPacket.Sent} > {PacketExpire.Ticks}");
                }
            }
            return pendingPacket;
        }
    }
}
