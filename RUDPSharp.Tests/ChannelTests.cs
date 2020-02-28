using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class ChannelTests
    {
        [Test]
        public void TestUnreliableChannelDoesNotDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded.
            var channel = new UnreliableChannel ();
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[0]));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,2, new byte[0]));

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
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,1, new byte[0]));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,3, new byte[10] {1,2,3,4,5,6,7,8,9,0}));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None,2, new byte[0]));

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
    }
}