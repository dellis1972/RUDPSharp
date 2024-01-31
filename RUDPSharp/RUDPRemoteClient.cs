using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace RUDPSharp
{
    public class RUDPRemoteClient<T> where T: UDPSocket  {

        Dictionary<Channel, UnreliableChannel> channels = new Dictionary<Channel, UnreliableChannel> ();
        bool isConnected = false;

        RUDP<T> client;
        public EndPoint RemoteEndPoint { get; private set; }

        public bool IsConnected => isConnected;

        public RUDPRemoteClient(RUDP<T> client, EndPoint remoteEndPoint)
        {
            channels[Channel.None] = new UnreliableChannel ();
            channels[Channel.InOrder] = new InOrderChannel ();
            channels[Channel.Reliable] = new ReliableChannel ();
            channels[Channel.ReliableInOrder] = new ReliableInOrderChannel ();
            this.client = client;
            RemoteEndPoint = remoteEndPoint;
        }

        public void QueueOutgoing (EndPoint remoteEndPoint, PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            var packet = new Packet (packetType, channel, payload);
            channels[packet.Channel].QueueOutgoingPacket (remoteEndPoint, packet);
        }

        public void QueueIncoming (EndPoint remoteEndPoint, byte[] data)
        {
            var packet = new Packet (data, data.Length);
            channels[packet.Channel].QueueIncomingPacket (remoteEndPoint, packet);
        }

        IEnumerable<PendingPacket> GetOutgoingPackets ()
        {
            foreach (var channel in channels)
                foreach (var packet in channel.Value.GetPendingOutgoingPackets ())
                    yield return packet;
        }

        IEnumerable<PendingPacket> GetIncomingPackets ()
        {
            foreach (var channel in channels)
                foreach (var packet in channel.Value.GetPendingIncomingPackets ())
                    yield return packet;
        }

        bool IsSystemPacket (PacketType packetType)
        {
            return packetType == PacketType.Connect || packetType == PacketType.Disconnect ||
                packetType == PacketType.Ping || packetType == PacketType.Pong;
        }

        bool HandleSystemPacket (PendingPacket packet)
        {
            switch (packet.PacketType)  {
                case PacketType.Connect:
                    if (!isConnected && client.ConnectionRequested != null && client.ConnectionRequested(packet.RemoteEndPoint, packet.Data)) {
                        isConnected = true;
                        QueueOutgoing (packet.RemoteEndPoint, PacketType.Connect, Channel.Reliable, Encoding.ASCII.GetBytes ("h2ik"));
                    }
                    break;
                case PacketType.Disconnect:
                    // return false tells the client we disconnected.
                    if (client.Disconnected != null)
                        client.Disconnected (packet.RemoteEndPoint);
                    isConnected = false;
                    return false;
                case PacketType.Ping:
                    QueueOutgoing (packet.RemoteEndPoint, PacketType.Pong, Channel.Reliable, packet.Data);
                    break;
                case PacketType.Pong:
                    long sent = BitConverter.ToInt64 (packet.Data, 0);
                    Debug.WriteLine ($"Ping/Pong time {TimeSpan.FromTicks (DateTime.Now.Ticks - sent).TotalMilliseconds} ms");
                    // We are still alive :) 
                    break;
            }
            return true;
        }

        public bool SendAndReceive (Func<EndPoint, byte [], bool> func)
        {
            foreach (var packet in GetIncomingPackets ()) {
                // do something...
                if (IsSystemPacket (packet.PacketType)) {
                    if (!HandleSystemPacket (packet))
                        return false;
                    continue;
                }
                if (!isConnected && packet.PacketType != PacketType.UnconnectedMessage) {
                    Debug.WriteLine ($"Dropping packet from {packet.RemoteEndPoint} {packet.Channel} {packet.PacketType} {packet.Sequence}. Not Connected.");
                    continue;
                }
                if (func != null && !func (packet.RemoteEndPoint, packet.Data)) {
                    
                }
            }
            foreach (var packet in GetOutgoingPackets ()) {
                client.Send (packet.RemoteEndPoint, packet.Data);
            }
            return true;
        }
    }
}