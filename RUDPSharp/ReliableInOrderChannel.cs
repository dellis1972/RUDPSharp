using System;
using System.Collections.Generic;
using System.Net;

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

        protected override bool CheckSequence(int packet, int sequence)
        {
            return packet == sequence+1 || sequence == -1;
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
                if (pendingPackets.Keys[i] < RemoteSequence || (now - pendingPackets.Values[0].Sent > PacketExpire.Ticks)) {
                    pendingPackets.RemoveAt (i);
                }
            }
            return pendingPacket;
        }
    }
}
