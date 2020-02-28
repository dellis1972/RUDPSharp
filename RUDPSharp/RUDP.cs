using System;
using System.Collections.Generic;
using System.Net;

namespace RUDPSharp
{
    public class RUDPRemoteClient {
        UnreliableChannel unreliableChannel = new UnreliableChannel ();
        InOrderChannel inOrderChannel = new InOrderChannel ();
        public EndPoint RemoteEndPoint { get; }

        public void QueueOutgoing (PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            var packet = new Packet (packetType, channel, payload);
            switch (channel)
            {
                case Channel.InOrder:
                    unreliableChannel.QueueOutgoingPacket (packet);
                    break;
                case Channel.Reliable:
                    break;
                case Channel.ReliableInOrder:
                    break;
                default:
                    unreliableChannel.QueueOutgoingPacket (packet);
                    break;
            }
        }

        public void QueueIncoming (byte[] data)
        {
            var packet = new Packet (data);
            switch (packet.Channel) {
                case Channel.InOrder:
                    unreliableChannel.QueueIncomingPacket (packet);
                    break;
                case Channel.Reliable:
                    break;
                case Channel.ReliableInOrder:
                    break;
                default:
                    unreliableChannel.QueueIncomingPacket (packet);
                    break;
            }
        }
    }
    public class RUDP<T> where  T : UDPSocket {

        T socket;
        Dictionary<EndPoint, RUDPRemoteClient> remotes = new Dictionary<EndPoint, RUDPRemoteClient> ();

        public RUDP (T socket)
        {
            this.socket = socket;
        }

        public void Start (int port)
        {
            socket.Listen (port);
        }

        protected void SendTo (EndPoint endPoint, PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            remotes[endPoint].QueueOutgoing (packetType, channel, payload);
        }

        protected virtual void ProcessData (EndPoint remoteEndPoint, byte[] data)
        {
                remotes[remoteEndPoint].QueueIncoming (data);
        }
    }
}