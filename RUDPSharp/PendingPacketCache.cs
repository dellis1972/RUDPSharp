using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using static RUDPSharp.UnreliableChannel;

namespace RUDPSharp
{
    /// <summary>
    /// This class handles the caching of early packets for the ReliableInOrderChannel
    /// </summary>
    class PendingPacketCache
    {
        SortedList<int, PendingPacket> pendingPackets = new SortedList<int, PendingPacket> ();
        ConcurrentQueue<int> expired = new ConcurrentQueue<int> ();

        public TimeSpan PacketExpire {get ;set; } = TimeSpan.FromMilliseconds(500);

        public bool HasPacketsToDeliver => pendingPackets.Count > 0;
        public bool TryCachePacket (PendingPacket pendingPacket)
        {
            if (pendingPackets.IndexOfKey (pendingPacket.Sequence) == -1) {
                pendingPackets.Add (pendingPacket.Sequence, pendingPacket);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Checks the cache for any packets which should be delivered BEFORE the
        /// next packet.
        /// </summary>
        /// <param name="packet">The latest packet to arrive.</param>
        /// <returns>An Enumerable of PendingPackets to process.</returns>
        public bool TryGetPacketToDeliver(int sequence, out PendingPacket pendingPacket)
        {
            int currentSequence = sequence;
            long now = DateTime.Now.Ticks;
            pendingPacket = null;
            foreach (var pending in pendingPackets) {
                if (now - pending.Value.Sent > PacketExpire.Ticks) {
                    expired.Enqueue (pending.Key);
                    Debug.WriteLine ($"Dropping Packet from {pending.Value.RemoteEndPoint}. Packet is too old {now - pending.Value.Sent} > {PacketExpire.Ticks}");
                    continue;
                }
                // -1 means we have no packets in the queue just deliver what you have.
                if (pending.Key < currentSequence || currentSequence == -1) {
                    pendingPacket = pending.Value;
                    expired.Enqueue (pending.Key);
                    break;
                }
            }
            while (expired.Count > 0) {
                if (expired.TryDequeue (out int i))
                    pendingPackets.Remove (i);
            }
            return pendingPacket != null;
        }
    }
}