using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace RUDPSharp
{
    public class InOrderChannel : UnreliableChannel {
        int sequence = 0;
        int remoteSequence = 0;
        // Makes sure packets arrive in order and discards old packets
        protected int Sequence => sequence;
        protected int RemoteSequence => remoteSequence;
        protected virtual bool CheckSequence(int packet, int sequence)
        {
            return packet > sequence;
        }

        protected virtual bool QueueOrDiscardPendingPackages(EndPoint endPoint, PendingPacket packet)
        {
            if (CheckSequence (packet.Sequence, remoteSequence)) {
                remoteSequence = packet.Sequence;
                return true;
            }
            return false;
        }

        public InOrderChannel(int maxBufferSize = 100) : base (maxBufferSize)
        {
            
        }

        public override PendingPacket QueueOutgoingPacket (EndPoint endPoint, Packet packet)
        {
            if (packet.Sequence == 0)
                packet.Sequence = (ushort)++sequence;
            return base.QueueOutgoingPacket (endPoint, packet);;
        }
        public override PendingPacket QueueIncomingPacket (EndPoint endPoint, Packet packet)
        {
            if (QueueOrDiscardPendingPackages (endPoint, PendingPacket.FromPacket (endPoint, packet))) {
                return base.QueueIncomingPacket (endPoint, packet);
            }
            Console.WriteLine ($"Dropping Packet from {endPoint}. Packet is too old {packet.Sequence} < {sequence}");
            return null;
        }
    }
}