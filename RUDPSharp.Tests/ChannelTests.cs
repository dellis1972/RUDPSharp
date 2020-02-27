using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestUnreliableChannelDiscardsOldMessages()
        {
            // Create 3 packets, add them in the order 1, 3, 2. 
            // 2 should be discarded.
            var channel = new UnreliableChannel ();
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None, new byte[0]));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None, new byte[0]));
            channel.QueueIncomingPacket (new Packet (PacketType.UnconnectedMessage, Channel.None, new byte[0]));

            UnreliableChannel.PendingPacket packet;
            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (1, packet.Sequence);

            Assert.IsTrue (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNotNull (packet);
            Assert.AreEqual (1, packet.Sequence);

            Assert.IsFalse (channel.TryGetNextIncomingPacket (out packet));
            Assert.IsNull (packet);
        }
    }
}