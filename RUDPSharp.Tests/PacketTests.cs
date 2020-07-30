using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class PacketTests
    {
        [Test]
        [TestCase (PacketType.Ack, Channel.Reliable, 1, new byte[2] {128, 255})]
        [TestCase (PacketType.Connect, Channel.None, 2, new byte[2] {128, 255})]
        [TestCase (PacketType.Disconnect, Channel.None, 5, new byte[2] {128, 255})]
        [TestCase (PacketType.Ping, Channel.Reliable, ushort.MaxValue-1, new byte[2] {128, 255})]
        [TestCase (PacketType.Pong, Channel.ReliableInOrder, ushort.MaxValue-5, new byte[2] {128, 255})]
        [TestCase (PacketType.UnconnectedMessage, Channel.None, 1231, new byte[2] {128, 255})]
        public void PacketTest(PacketType packetType, Channel channel, int sequence, byte[] payload)
        {
            var p1 = new Packet (packetType, channel, (ushort)sequence, payload);
            var p2 = new Packet (p1.Data, p1.Data.Length);

            Assert.AreEqual (p1.Channel, p2.Channel, $"{p1.Channel} != {p2.Channel}");
            Assert.AreEqual (p1.PacketType, p2.PacketType, $"{p1.PacketType} != {p2.PacketType}");
            Assert.AreEqual (p1.Sequence, p2.Sequence, $"{p1.Sequence} != {p2.Sequence}");
            Assert.AreEqual (p1.Payload.ToArray (), p2.Payload.ToArray (), $"({(string.Join (",", p1.Payload.ToArray ()))}) != ({(string.Join (",", p2.Payload.ToArray ()))})");
        }
    }
}