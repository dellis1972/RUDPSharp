using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RUDPSharp
{
public class UDPSocket : IDisposable {
        Socket socketIP4;
        Socket socketIP6;
        const int BufferSize =  1024;
        const int SioUdpConnreset = -1744830452; //SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12
        const int SocketTTL = 255;
        string name;

        BufferPool<byte> pool = new BufferPool<byte> (BufferSize, 10);
        SocketAsyncEventArgsPool<SocketAsyncEventArgs> sendArgsPool;

        BlockingCollection<(EndPoint remote, byte [] data)> recievedPackets = new BlockingCollection<(EndPoint remote, byte [] data)> ();

        public EndPoint EndPoint {
            get {
                return GetEndPoint ();
            }
        }

        public int MaxReceiveThreads { get; set; } = 10;

        public int SendTimeout { get; set; } = 500;

        public int ReceiveTimeout { get; set; } = 500;
        public BlockingCollection<(EndPoint remote, byte [] data)> ReceivedPackets => recievedPackets;

        void SetupSocket (Socket socket, bool reuseAddress = false)
        {
            socket.ReceiveTimeout = ReceiveTimeout;
            socket.SendTimeout = SendTimeout;
            socket.ReceiveBufferSize = BufferSize;
            socket.SendBufferSize = BufferSize;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                socket.IOControl(SioUdpConnreset, new byte[] {0}, null);
                socket.SetIPProtectionLevel (IPProtectionLevel.Unrestricted); // NAT??
            }
            socket.ExclusiveAddressUse = !reuseAddress;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: reuseAddress);
            if (socket.AddressFamily == AddressFamily.InterNetwork) {
                socket.Ttl = 255;
                try {
                    socket.DontFragment = true;
                } catch (SocketException) {
                }

                try {
                    socket.EnableBroadcast = true;
                } catch (SocketException) {
                }
            }
        }

        bool Bind (Socket socket, EndPoint endPoint)
        {
            try {
                socket.Bind (endPoint);
            } catch (SocketException ex) {
                switch (ex.SocketErrorCode) {
                    case SocketError.AddressAlreadyInUse:
                        if (socket.AddressFamily == AddressFamily.InterNetworkV6) {
                            socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, true);
                            socket.Bind(endPoint);
                            return socket.IsBound;
                        }
                        break;
                }
                return false;
            }
            Debug.WriteLine ($"{endPoint} Bound");
            return socket.IsBound;
        }

        public struct DataSent {
            public TaskCompletionSource<bool> TaskCompletion;
            public CancellationTokenRegistration Registration;
        }

        void Sent (object sender, SocketAsyncEventArgs e)
        {
            var tcs = (DataSent)e.UserToken;
            using (tcs.Registration) {
                try {
                if (!tcs.TaskCompletion.Task.IsCanceled && !tcs.TaskCompletion.Task.IsFaulted)
                    tcs.TaskCompletion.TrySetResult (e.SocketError == SocketError.Success);
                } catch (Exception ex) {
                    Debug.WriteLine (ex);
                }
            }
            sendArgsPool.Return(e);
        }

        public struct DataReceived {
            public TaskCompletionSource<(EndPoint remote, byte [] data, int length)> TaskCompletion;
            public System.Threading.CancellationTokenRegistration Registration;
        }

        void Received (object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
                BeginReceiving (e);
            byte[] data = new byte[e.BytesTransferred];
            Buffer.BlockCopy (e.Buffer, 0, data, 0, e.BytesTransferred);
            if (!recievedPackets.TryAdd ((e.RemoteEndPoint, data)))
                return;

            BeginReceiving (e);
        }

        public UDPSocket(string name = "UDPSocket")
        {
            this.name = name;
            sendArgsPool = new SocketAsyncEventArgsPool<SocketAsyncEventArgs> (MaxReceiveThreads, Sent);
        }

        public virtual void Initialize ()
        {
            if (socketIP4 != null)
                return;
            socketIP4 = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketIP6 = new Socket (AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            SetupSocket (socketIP4, reuseAddress: true);
            SetupSocket (socketIP6, reuseAddress: true);
        }

        public virtual bool Listen (int port)
        {
            var ep = new IPEndPoint (IPAddress.Any, port);
            bool result = Bind (socketIP4, ep);
            Debug.WriteLine ($"{name} is Listening on {ep} {result}");
            var epV6 = new IPEndPoint (IPAddress.IPv6Any, port);
            result &= Bind (socketIP6, epV6);
            for (int i=0;i<MaxReceiveThreads;i++) {
                var receiveAsyncArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = ep
                };
                var buffer = pool.Rent (socketIP4.ReceiveBufferSize);
                receiveAsyncArgs.SetBuffer (buffer, 0, buffer.Length);
                receiveAsyncArgs.Completed += Received;
                BeginReceiving (receiveAsyncArgs);
            }
            return result;
        }

        void BeginReceiving (SocketAsyncEventArgs receiveAsyncArgs)
        {
            if (receiveAsyncArgs.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork && (!socketIP4.ReceiveFromAsync (receiveAsyncArgs))) {
                Received (this, receiveAsyncArgs);
            }
        }

        public virtual void ReturnBuffer (byte[] buffer){
            pool.Return (buffer);
        }

        public virtual Task<bool> SendTo (EndPoint endPoint, byte[] data, System.Threading.CancellationToken token)
        {
            SocketAsyncEventArgs sendAsyncArgs = sendArgsPool.Rent ();
            TaskCompletionSource<bool> sendTcs = new TaskCompletionSource<bool> ();
            var registration = token.Register (() => sendTcs.TrySetCanceled ());
            sendAsyncArgs.RemoteEndPoint = endPoint;
            sendAsyncArgs.SetBuffer (data, 0, data.Length);
            sendAsyncArgs.UserToken = new DataSent {
                TaskCompletion = sendTcs,
                Registration = registration,
            };
            if (endPoint.AddressFamily == AddressFamily.InterNetwork && (!socketIP4.SendToAsync (sendAsyncArgs))) {
                Sent (this, sendAsyncArgs);
            }
            return sendTcs.Task;
        }

        protected virtual EndPoint GetEndPoint ()
        {
            return  socketIP4?.LocalEndPoint ?? socketIP6.LocalEndPoint;;
        }

        public void Complete ()
        {
            if (!recievedPackets.IsAddingCompleted)
                recievedPackets.CompleteAdding ();
        }

        public void Dispose()
        {
            try {
                if (socketIP4 != null) {
                    socketIP4.Close ();
                }
            } catch {

            }
            Complete ();
            try {
                if (socketIP6 != null) {
                    socketIP6.Close ();
                }
            } catch {
                
            }
        }
    }
}