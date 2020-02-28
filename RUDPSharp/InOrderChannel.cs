using System;
using System.Collections.Generic;

namespace RUDPSharp
{
    public class InOrderChannel : UnreliableChannel {
        int sequence = 0;
        // Makes sure packets arrive in order and discards old packets
        public override void QueueIncomingPacket (Packet packet)
        {
            if (packet.Sequence > sequence) {
                sequence = packet.Sequence;
                base.QueueIncomingPacket (packet);
            }
        }
    }
}