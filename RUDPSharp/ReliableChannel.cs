using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace RUDPSharp
{
    public class ReliableChannel : InOrderChannel {
        PacketAcknowledgement acknowledgement = new PacketAcknowledgement();
        public ReliableChannel(int maxBufferSize = 100) : base (maxBufferSize)
        {
        }

        public override PendingPacket QueueOutgoingPacket(EndPoint endPoint, Packet packet)
        {
            var pendingPacket = base.QueueOutgoingPacket (endPoint, packet);
            acknowledgement.HandleOutgoingPackage(packet.Sequence, pendingPacket);
            return pendingPacket;
        }
        public override PendingPacket QueueIncomingPacket (EndPoint endPoint, Packet packet)
        {
            if (!acknowledgement.HandleIncommingPacket(packet))
            {
                QueueOutgoingPacket(endPoint, new Packet(PacketType.Ack, packet.Channel, packet.Sequence, new byte[0]));
                return base.QueueIncomingPacket (endPoint, packet);
            }
            // ignore Ack Packets
            return null;
        }

        public override IEnumerable<PendingPacket> GetPendingOutgoingPackets ()
        {
            foreach (var p in acknowledgement.GetPacketsToResent())
                yield return p;
            foreach (var p in base.GetPendingOutgoingPackets())
                yield return p;
        }
    }
}