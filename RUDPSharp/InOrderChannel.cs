using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;

namespace RUDPSharp
{
    public class InOrderChannel : UnreliableChannel {
        const ushort MAX_SEQUENCE_NUMBER = ushort.MaxValue  - 1;
        int sequence = 0;
        int remoteSequence = 0;
        // Makes sure packets arrive in order and discards old packets
        protected int Sequence => sequence;
        protected int RemoteSequence => remoteSequence;

        protected virtual int CheckSequence(int packet, int sequence)
        {
            if (packet == sequence)
                return 0;
            if ((packet < sequence && (sequence - packet) < MAX_SEQUENCE_NUMBER / 2)
                || (packet > sequence && (packet - sequence) > MAX_SEQUENCE_NUMBER / 2))
            {
                return 1;
            }
            return -1; 
        }

        protected virtual int GetNextSequence(int sequence)
        {
            return  (sequence + 1) % MAX_SEQUENCE_NUMBER;
        }

        protected virtual bool QueueOrDiscardPendingPackages(EndPoint endPoint, PendingPacket packet)
        {
            if (CheckSequence (packet.Sequence, remoteSequence) == -1) {
                remoteSequence = packet.Sequence;
                return true;
            }
            return false;
        }

        public InOrderChannel(int maxBufferSize = 1024) : base (maxBufferSize)
        {

        }

        public override PendingPacket QueueOutgoingPacket (EndPoint endPoint, Packet packet)
        {
            if (packet.Sequence == 0) {
                sequence = GetNextSequence(sequence);
                packet.Sequence = (ushort)sequence;
            }
            return base.QueueOutgoingPacket (endPoint, packet);
        }
        public override PendingPacket QueueIncomingPacket (EndPoint endPoint, Packet packet)
        {
            if (QueueOrDiscardPendingPackages (endPoint, PendingPacket.FromPacket (endPoint, packet))) {
                return base.QueueIncomingPacket (endPoint, packet);
            }
            Debug.WriteLine ($"Dropping Packet from {endPoint}. Packet is too old {packet.Sequence} < {sequence}");
            return null;
        }
    }
}