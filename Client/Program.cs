using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client
{
    class Program
    {
        
        static void Main(string[] args)
        {
            int serverPort = 8002;
            int clientPort = 8003;
            bool done = false;
            List<Task> tasks = new List<Task> ();
            System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource ();
            using (var server = new UDPSocket ("Server")) {
                server.Listen (serverPort);
                tasks.Add (Task.Run (async () => {
                    int i = 50000;
                    try {
                        while (!done) {
                            var data = await server.ReceiveFrom (new IPEndPoint(IPAddress.Any, serverPort), cts.Token);
                            if (data.length > 0) {
                                server.ReturnBuffer (data.data);
                                await server.SendTo (data.remote, Encoding.ASCII.GetBytes ($"ACK{i++}"), cts.Token);
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine(ex);
                    } finally {
                        Console.WriteLine ($"Server {i-50000}");
                    }
                }));
                tasks.Add (Task.Run ( async () => {
                    int i = 20000;
                    try {
                        using (var socket = new UDPSocket ($"Client 1")) {
                            socket.Listen (clientPort+1);
                            var local = new IPEndPoint (IPAddress.Loopback, serverPort);
                            await socket.SendTo (local, Encoding.ASCII.GetBytes ("Hello"), cts.Token);
                            
                            while (!done) {
                                var data = await socket.ReceiveFrom (local, cts.Token);
                                if (data.length > 0) {
                                    //Console.WriteLine ( $"Got {Encoding.ASCII.GetString (data.data)} from {data.remote}");
                                    socket.ReturnBuffer (data.data);
                                    await socket.SendTo (data.remote, Encoding.ASCII.GetBytes ($"ACK{i++}"), cts.Token);
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine(ex);
                    } finally {
                        Console.WriteLine ($"Client 1 {i-20000}");
                    }
                }));

                TimeSpan ts = TimeSpan.FromSeconds (5);
                while (!done) {
                    System.Threading.Thread.Sleep (10);
                    ts = ts.Add (-TimeSpan.FromMilliseconds (10));
                    if (ts.TotalMilliseconds < 0) {
                        done = true;
                        cts.Cancel ();
                        System.Threading.Thread.Sleep(100);
                        try {
                        Task.WaitAll (tasks.ToArray ());
                        } catch {
                            // ignore we will get a cancelled error
                        }
                        Console.WriteLine ("Done");
                    }
                }
            }
        }
    }

    public enum PacketType : byte {
        UnconnectedMessage = 0x0,
        Ack = 0x01, // Acknowledge recept of a packet
        Ping = 0x02,// A Ping, respond with a Pong
        Pong = 0x03,// A response to a Ping, used to track packet delivery times
        Connect = 0x04,// A new client wants to connect
        Disconnect = 0x05,// A client wants to disconnect.
    }

    public enum Channel : byte {
        // Just send the data and hope it gets there.
        None = 0x00,
        // Send the data and hope it gets there. If and old packet arrives disgard it.
		InOrder = 0x01,
        // Send the data and wait for an ACK packet. If one does not arrive resend. If an old packet arrives disgard it,
		Reliable = 0x02,
        // Send the data and wait for an ACK packet. Packets will be processed in sequence order.
        // if a packet arrives and a sequence is skipped it will be queued for later use.
		ReliableInOrder = Reliable | InOrder,
    }

    public ref struct Packet {
        byte [] rawData;
        Span<byte> span;

        public Packet (byte[] data)
        {
            rawData = data;
            span = new Span<byte>(rawData);
            var header = DecodeHeader (rawData[0]);
            PacketType = header.type;
            Channel = header.channel;
        }

        public Packet (PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            byte header = EncodeHeader (packetType, channel);
            rawData = new byte[payload.Length + 1];
            rawData[0] = header;
            for (int i=0; i< payload.Length; i++) 
                rawData[i-1] = payload[i];
            span = new Span<byte> (rawData);
            PacketType = packetType;
            Channel = channel;
        }

        public byte Header { get { return span[0]; }}

        public PacketType PacketType { get; private set; }

        public Channel Channel { get; private set; }

        public ReadOnlySpan<byte> Payload { get { return span.Slice (1); }}

        public byte[] Data { get { return rawData; }}

        static byte EncodeHeader(PacketType type, Channel channel)
        {   
            byte t = (byte)type;
            byte t1 = (byte)channel;
            byte shift = 4;
            byte header = (byte)(t1 << shift); 
            header = (byte)(header | t);
            return header;
        }

        static (PacketType type, Channel channel) DecodeHeader (byte header)
        {
            return (type : (PacketType)(header & 0x1F), channel : (Channel)(header >> 4));
        }

    }

    public class UnreliableChannel {

        internal class PendingPacket {
            public PacketType PacketType { get; private set; }
            public Channel Channel { get; private set; }

            public byte [] Data { get; private set; }

            public static PendingPacket FromPacket (Packet packet)
            {
                return new PendingPacket () {
                    Channel = packet.Channel,
                    PacketType = packet.PacketType,
                    Data = packet.Payload.ToArray (),
                };
            }
        }
        Queue<PendingPacket> outgoing = new Queue<PendingPacket> (100);
        Queue<PendingPacket> incoming = new Queue<PendingPacket> (100);
        // Unreliable
        public virtual void QueueOutgoingPacket (Packet packet)
        {
            outgoing.Enqueue (PendingPacket.FromPacket (packet));
        }

        public virtual void QueueIncomingPacket (Packet packet)
        {
            incoming.Enqueue (PendingPacket.FromPacket (packet));
        }
    }

    public class InOrderChannel : UnreliableChannel {
        // Makes sure packets arrive in order and discards old packets
    }

    public class ReliableChannel : InOrderChannel {
        // Adds ACK support on top of InOrderChannel but still discards old packets
    }

    public class ReliableInOrderChannel : ReliableChannel {
        // Same as ReliableChannel but will keep all packets and deliver in order.
    }

    public class UDP {

        UDPSocket socket;
        List<EndPoint> remotes = new List<EndPoint> ();

        public UDP ()
        {
            socket = new UDPSocket ();
        }

        public void Start (int port)
        {
            socket.Listen (port);
        }

        protected void SendTo (EndPoint endPoint, PacketType packetType, Channel channel, ReadOnlySpan <byte> packetData)
        {
            var packet = new Packet (packetType, channel, packetData);
            socket.SendTo (endPoint, packet.Data, default);
            switch (channel) {
                case Channel.InOrder:
                break;
                case Channel.Reliable:
                break;
                case Channel.ReliableInOrder:
                break;
            }
        }

        protected virtual void ProcessData (EndPoint remoteEndPoint, byte[] data)
        {
                var packet = new Packet (data);
                switch (packet.Channel) {
                    case Channel.Reliable:
                        // we need to send an ACK
                        break;
                    case Channel.InOrder:
                        // maybe we need to queue this packet or drop it
                        break;
                    case Channel.ReliableInOrder:
                        // we need to send an ACK
                        break;
                }
                switch (packet.PacketType) {
                    case PacketType.Ack:
                        // packet acknowledgement yay :D clean up this item from the backlog
                        break;
                    case PacketType.Ping:
                        SendTo (remoteEndPoint, PacketType.Pong, Channel.Reliable, packet.Payload);
                        break;
                    case PacketType.Pong:
                        // process the packet calculate times
                        break;
                    case PacketType.Connect:
                        // new user
                        remotes.Add (remoteEndPoint);
                        break;
                    case PacketType.Disconnect:
                        // remove user
                        remotes.Remove (remoteEndPoint);
                        break;
                }
        }
    }
}
