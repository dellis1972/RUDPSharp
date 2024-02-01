using System;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class ChannelTests
    {
        IPEndPoint EndPoint = new IPEndPoint(IPAddress.Any, 8000);

        [Test]
        [Ignore ("Not implemented")]
        public void TestUnreliableChannelHandlesFragmentedPackages ()
        {
            var channel = new UnreliableChannel ();
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[5] {1,2,3,4,5}, fragmented: true));

            PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet), "Should have got packet 1");
            Assert.IsNotNull (packet, "Packet 1 should not be null.");
            Assert.AreEqual (1, packet.Sequence, $"Packet sequence should be 1 but was {packet.Sequence}");
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data, "Packet 1 data did not match.");

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNull (packet, "We should not have got the fragmented packet yet.");

            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,2, new byte[5] {6,7,8,9,0}, fragmented: true));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,4, new byte[10] {1,2,3,4,5,6,7,8,9,0}));

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (2, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet), "Should have got packet 1");
            Assert.IsNotNull (packet, "Packet 4 should not be null.");
            Assert.AreEqual (4, packet.Sequence, $"Packet sequence should be 4 but was {packet.Sequence}");
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data, "Packet 4 data did not match.");

            
        }

        [Test]
        public void TestUnreliableChannelDoesNotDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded.
            var channel = new UnreliableChannel ();
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,3, Array.Empty<byte> ()));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,2, Array.Empty<byte> ()));

            PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet), "Should have got packet 1");
            Assert.IsNotNull (packet, "Packet 1 should not be null.");
            Assert.AreEqual (1, packet.Sequence, $"Packet sequence should be 1 but was {packet.Sequence}");
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data, "Packet 1 data did not match.");

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (3, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (2, packet.Sequence);
        }

        [Test]
        public void TestInOrderChannelDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded.
            var channel = new InOrderChannel ();
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,1, Array.Empty<byte> ()));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,2, Array.Empty<byte> ()));

            PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet), "Packet 1 was not received.");
            Assert.IsNotNull (packet);
            Assert.AreEqual (1, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet), "Packet 3 was not received.");
            Assert.IsNotNull (packet);
            Assert.AreEqual (3, packet.Sequence);
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data);

            Assert.IsFalse (channel.TryGetNextIncomingPacket (out packet), "Packet 2 was not ignored.");
            Assert.IsNull (packet);
        }

        [Test]
        public void TestReliableChannelDoesNotDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded 
            var channel = new ReliableChannel ();

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,1, Array.Empty<byte> ()));
            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,3, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,2, Array.Empty<byte> ()));

            PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (1, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (3, packet.Sequence);
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data);

            Assert.IsFalse(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNull(packet);
        }

        [Test]
        public void TestReliableInOrderChannelDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be delivered as long as it did not expire
            var channel = new ReliableInOrderChannel();

            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 1, Array.Empty<byte> ()));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 3, new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 2, Array.Empty<byte> ()));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 4, Array.Empty<byte> ()));

            PendingPacket packet;
            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet), "Should have got packet 1");
            Assert.IsNotNull(packet);
            Assert.AreEqual(1, packet.Sequence);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet), "Should have got packet 2");
            Assert.IsNotNull(packet);
            Assert.AreEqual(2, packet.Sequence);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet), "Should have got packet 3");
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Sequence);
            Assert.AreEqual(new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, packet.Data);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet), "Should have got packet 4");
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Sequence);

            Assert.IsFalse(channel.TryGetNextIncomingPacket(out packet), "Should not have got a packet");
            Assert.IsNull(packet);
        }

        [Test]
        public void TestInOrderChannelHandlesSequenceWrapping ()
        {
            var rnd = new Random ();
            var outChannel = new InOrderChannel ();
            var inChannel = new InOrderChannel ();
            int[] sequences = [1, 534, 5346, 15346, 25676, 35246, 45646, 55366, 65532, 50, 4560];
            foreach (var sequence in sequences) {
                PendingPacket outPacket;
                ushort expectedSequence = (ushort)((sequence % (ushort.MaxValue)));
                do {
                    // get a bunch of packets and discard them
                    outChannel.QueueOutgoingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,0, Array.Empty<byte> ()));
                    Assert.IsTrue (outChannel.TryGetNextOutgoingPacket (out outPacket));
                    Assert.IsTrue (outPacket.Sequence < (ushort.MaxValue -1));
                } while (outPacket.Sequence != expectedSequence);

                inChannel.QueueIncomingPacket (EndPoint, new Packet (outPacket.PacketType, Channel.None, (ushort)outPacket.Sequence, outPacket.Data, false));
                Assert.IsTrue (inChannel.TryGetNextIncomingPacket (out PendingPacket packet), $"Should have got a packet for {sequence}");
                Assert.AreEqual (expectedSequence, packet.Sequence, $"Packet sequence should be {expectedSequence} but was {packet.Sequence}");
            }
        }

        [Test]
        public void TestReliableChannelResends () {
            var channel = new ReliableChannel ();

            channel.QueueOutgoingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 1, Array.Empty<byte> ()));

            PendingPacket[] packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsNotEmpty (packets);
            Assert.AreEqual(1, packets[0].Sequence);

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Ack, Channel.Reliable, (ushort)packets[0].Sequence, Array.Empty<byte> ()));
            packets = channel.GetPendingIncomingPackets().ToArray ();
            Assert.IsEmpty (packets);

            channel.QueueOutgoingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 2, Array.Empty<byte> ()));
            packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsNotEmpty (packets);
            Assert.AreEqual(2, packets[0].Sequence);

            Thread.Sleep (600);

            packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsNotEmpty (packets);
            Assert.AreEqual(2, packets[0].Sequence);

            Thread.Sleep (600);

            packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsNotEmpty (packets);
            Assert.AreEqual(2, packets[0].Sequence);

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Ack, Channel.Reliable, (ushort)packets[0].Sequence, Array.Empty<byte> ()));
            packets = channel.GetPendingIncomingPackets().ToArray ();
            Assert.IsEmpty (packets);

            packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsEmpty (packets);
        }

        [Test]
        public void TestReliableInOrderChannelHandlesSequenceWrapping ()
        {
            var rnd = new Random ();
            var outChannel = new ReliableInOrderChannel ();
            var inChannel = new ReliableInOrderChannel ();
            int[] sequences = [1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 10, 11];
            foreach (var sequence in sequences) {
                PendingPacket outPacket;
                ushort expectedSequence = (ushort)((sequence % (ushort.MaxValue)));
                do {
                    // get a bunch of packets and discard them
                    outChannel.QueueOutgoingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,0, Array.Empty<byte> ()));
                    Assert.IsTrue (outChannel.TryGetNextOutgoingPacket (out outPacket));
                    Assert.IsTrue (outPacket.Sequence < (ushort.MaxValue -1));
                } while (outPacket.Sequence != expectedSequence);

                Thread.Sleep (600);
                inChannel.QueueIncomingPacket (EndPoint, new Packet (outPacket.PacketType, Channel.None, (ushort)outPacket.Sequence, outPacket.Data, false));
                Assert.IsTrue (inChannel.TryGetNextIncomingPacket (out PendingPacket packet), $"Should have got a packet for {sequence}");
                Assert.AreEqual (expectedSequence, packet.Sequence, $"Packet sequence should be {expectedSequence} but was {packet.Sequence}");
            }
        }
    }
}