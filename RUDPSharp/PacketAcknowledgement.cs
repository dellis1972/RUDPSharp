using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using static RUDPSharp.UnreliableChannel;

namespace RUDPSharp
{
    /// <summary>
    /// This class handles the outgoing/incoming packet acknowledgements
    /// </summary>
    class PacketAcknowledgement
    {
        ConcurrentDictionary<int, PendingPacket> sent = new ConcurrentDictionary<int, PendingPacket>();
        ConcurrentQueue<int> expired = new ConcurrentQueue<int> ();

        public TimeSpan PacketExpire {get ;set; } = TimeSpan.FromMilliseconds(500);
        public int ResentCount  {get;set;} = 3;

        public bool HandleIncommingPacket(Packet packet)
        {
            if (packet.PacketType == PacketType.Ack)
            {
                // find the item in "sent" and remove it.
                if (sent.TryGetValue(packet.Sequence, out PendingPacket pendingPacket))
                {
                    sent.TryRemove(packet.Sequence, out PendingPacket _);
                }
                return true;
            }
            while (expired.Count > 0) {
                if (expired.TryDequeue (out int i))
                    sent.TryRemove (i, out PendingPacket _);
            }
            return false;
        }

        public bool HandleOutgoingPackage(ushort sequence, PendingPacket pendingPacket)
        {
            if (pendingPacket.PacketType != PacketType.Ack)
            {
                sent.TryAdd(sequence, pendingPacket);
                return true;
            }
            return false;
        }

        public IEnumerable<PendingPacket> GetPacketsToResent()
        {
            long now = DateTime.Now.Ticks;
            foreach (var s in sent)
            {
                if (now - s.Value.Sent > PacketExpire.Ticks)
                {
                    s.Value.Sent = now;
                    s.Value.Attempts++;
                    if (s.Value.Attempts > ResentCount) {
                        expired.Enqueue (s.Key);
                        continue;
                    }
                    Debug.WriteLine ($"Resending Packet {s.Value.Channel} {s.Value.Sequence}");
                    yield return s.Value;
                }
            }
        }
    }
}
