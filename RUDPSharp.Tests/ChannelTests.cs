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
        public void TestUnreliableChannelDoesNotDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded.
            var channel = new UnreliableChannel ();
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[0]));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,2, new byte[0]));

            UnreliableChannel.PendingPacket packet;
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
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[0]));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (EndPoint, new Packet (PacketType.UnconnectedMessage, Channel.None,2, new byte[0]));

            UnreliableChannel.PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (1, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (3, packet.Sequence);
            Assert.AreEqual (new byte[10] {1,2,3,4,5,6,7,8,9,0}, packet.Data);

            Assert.IsFalse (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNull (packet);
        }

        [Test]
        public void TestReliableChannelDoesNotDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded 
            var channel = new ReliableChannel ();

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,1, new byte[0]));
            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,3, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Connect, Channel.Reliable,2, new byte[0]));

            UnreliableChannel.PendingPacket packet;
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
            // 2 should be discarded.
            var channel = new ReliableInOrderChannel();

            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 1, new byte[0]));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 3, new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 2, new byte[0]));
            channel.QueueIncomingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 4, new byte[0]));

            UnreliableChannel.PendingPacket packet;
            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNotNull(packet);
            Assert.AreEqual(1, packet.Sequence);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNotNull(packet);
            Assert.AreEqual(2, packet.Sequence);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNotNull(packet);
            Assert.AreEqual(3, packet.Sequence);
            Assert.AreEqual(new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, packet.Data);

            Assert.IsTrue(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNotNull(packet);
            Assert.AreEqual(4, packet.Sequence);

            Assert.IsFalse(channel.TryGetNextIncomingPacket(out packet));
            Assert.IsNull(packet);
        }

        [Test]
        public void TestReliableChannelResends () {
            var channel = new ReliableChannel ();

            channel.QueueOutgoingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 1, new byte[0]));

            UnreliableChannel.PendingPacket[] packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsNotEmpty (packets);
            Assert.AreEqual(1, packets[0].Sequence);

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Ack, Channel.Reliable, (ushort)packets[0].Sequence, new byte[0]));
            packets = channel.GetPendingIncomingPackets().ToArray ();
            Assert.IsEmpty (packets);

            channel.QueueOutgoingPacket(EndPoint, new Packet(PacketType.Connect, Channel.Reliable, 2, new byte[0]));
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

            channel.QueueIncomingPacket(EndPoint, new Packet (PacketType.Ack, Channel.Reliable, (ushort)packets[0].Sequence, new byte[0]));
            packets = channel.GetPendingIncomingPackets().ToArray ();
            Assert.IsEmpty (packets);

            packets = channel.GetPendingOutgoingPackets().ToArray ();
            Assert.IsEmpty (packets);
        }
    }
}