using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace RUDPSharp
{
    public class RUDP<T> : IDisposable where  T : UDPSocket {

        T socket;
        int port;
        ConcurrentDictionary<EndPoint, RUDPRemoteClient<T>> remotes = new ConcurrentDictionary<EndPoint, RUDPRemoteClient<T>> ();
        Task poll;
        Task readSocket;

        internal UDPSocket Socket => socket;

        CancellationTokenSource tokenSource = new CancellationTokenSource ();

        public Func<EndPoint, byte [] , bool> DataReceived { get; set; }

        public Func<EndPoint, byte [], bool> ConnectionRequested { get; set; }

        public Action<EndPoint> Disconnected { get; set; }

        public EndPoint EndPoint => socket.EndPoint;

        public ICollection<EndPoint> Remotes => remotes.Keys;

        public RUDP (T socket)
        {
            this.socket = socket;
            this.socket.Initialize ();
        }

        public void Start (int port)
        {
            this.port = port;
            socket.Listen (port);
            poll = Task.Run (Poll, tokenSource.Token);
            readSocket = Task.Run(ReadSocket, tokenSource.Token);
        }

        public bool Connect (string host, int port)
        {
            if (!IPAddress.TryParse (host, out IPAddress ip))
                return false;
            // Connect to remote server.
            var endPoint = new IPEndPoint (ip, port);
            SendTo (endPoint, PacketType.Connect, Channel.Reliable, Encoding.ASCII.GetBytes ("h2ik"));
            return true;
        }

        ///<summary>
        /// Attempt to connect to a remote server. Will return true if a connection is made , false otherwise.
        ///</summary>
        public Task<bool> ConnectAsync (string host, int port)
        {
            return new Task<bool> (() => false);
        }

        public bool Disconnect ()
        {
            foreach (var remote in remotes){
                SendTo (remote.Key, PacketType.Disconnect, Channel.Reliable, Encoding.ASCII.GetBytes ("Bye"));
            }
            Thread.Sleep(500);
            remotes.Clear ();
            return true;
        }

        public bool Disconnect (EndPoint endPoint)
        {
            SendTo (endPoint, PacketType.Disconnect, Channel.Reliable, Encoding.ASCII.GetBytes ("Bye"));
            Thread.Sleep(500);
            remotes.TryRemove (endPoint, out RUDPRemoteClient<T> client);
            return true;
        }

        public bool SendToAll (Channel channel, ReadOnlySpan<byte> payload)
        {
            foreach (var remote in remotes) {
                remote.Value.QueueOutgoing (remote.Key, PacketType.Data, channel, payload);
            }
            return true;
        }

        public bool SendTo (EndPoint endPoint, Channel channel, ReadOnlySpan<byte> payload)
        {
            if (remotes.TryGetValue (endPoint, out RUDPRemoteClient<T> client)) {
                client.QueueOutgoing (endPoint, PacketType.Data, channel, payload);
                return true;
            }
            return false;
        }

        protected void SendTo (EndPoint endPoint, PacketType packetType, Channel channel, ReadOnlySpan <byte> payload)
        {
            if (!remotes.TryGetValue (endPoint, out RUDPRemoteClient<T> client)) {
                client = new RUDPRemoteClient<T> (this, endPoint);
                remotes.TryAdd (endPoint, client);
            }
            remotes[endPoint].QueueOutgoing (endPoint, packetType, channel, payload);
        }

        public void Ping ()
        {
            foreach (var remote in remotes) {
                remote.Value.QueueOutgoing (remote.Key, PacketType.Ping, Channel.None, BitConverter.GetBytes (DateTime.Now.Ticks));
            }
        }

        void ReadSocket ()
        {
            RUDPRemoteClient<T> client;
            try {
                while (!tokenSource.IsCancellationRequested) {
                    foreach (var incoming in socket.ReceivedPackets.GetConsumingEnumerable (tokenSource.Token)) {
                        if (!remotes.TryGetValue (incoming.remote, out client)) {
                            client = new RUDPRemoteClient<T> (this, incoming.remote);
                            remotes[incoming.remote] = client;
                        }
                        client.QueueIncoming (incoming.remote, incoming.data);
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine (ex);
            }
        }

        void Poll ()
        {
            RUDPRemoteClient<T> client;
            try {
                while (!tokenSource.IsCancellationRequested) {
                    List<EndPoint> disconnected = new List<EndPoint> ();
                    foreach (var remote in remotes) {
                        if (!remote.Value.SendAndReceive (DataReceived)) {
                            // client disconnect. 
                            disconnected.Add (remote.Key);
                        }
                    }
                    foreach (var d in disconnected)
                        remotes.TryRemove (d, out client);
                    Thread.Sleep(1);
                }
            } catch (TaskCanceledException){

            }
        }

        internal Task<bool> Send(EndPoint remoteEndPoint, byte[] data)
        {
            return socket.SendTo (remoteEndPoint, data, tokenSource.Token);
        }

        public void Dispose ()
        {
            socket.Complete ();
            tokenSource.Cancel ();
            if (poll != null) {
                try {
                    Console.WriteLine ($"DEBUG! {poll.Status}");
                    if (!poll.IsCanceled && !poll.IsCompleted)
                        poll.Wait ();
                } catch (TaskCanceledException) {
                    // ignore this.
                }
                poll.Dispose ();
                poll = null;
            }
            if (readSocket != null) {
                try {
                    Console.WriteLine ($"DEBUG! {readSocket.Status}");
                    if (!readSocket.IsCanceled && !poll.IsCompleted)
                        readSocket.Wait ();
                } catch (TaskCanceledException) {
                    // ignore this.
                }
                readSocket.Dispose ();
                readSocket = null;
            }
            if (socket != null){ 
                socket.Dispose ();
                socket = null;
            }
        }
    }
}