using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
public class UDPSocket : IDisposable {
        Socket socketIP4;
        //Socket socketIP6;
        const int BufferSize =  1024;
        const int SioUdpConnreset = -1744830452; //SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12
        const int SocketTTL = 255;
        private EndPoint endPointFrom = new IPEndPoint(IPAddress.Any, 0);
        string _name;

        byte[] emptyData = new byte[0];
        BufferPool<byte> pool = new BufferPool<byte> (BufferSize, 10);
        Pool<SocketAsyncEventArgs> argsPool = new Pool<SocketAsyncEventArgs> (10);

        public EndPoint EndPoint {
            get {
                return socketIP4?.LocalEndPoint;// ?? socketIP6.LocalEndPoint;
            }
        }

        void SetupSocket (Socket socket, bool reuseAddress = false)
        {
            socket.ReceiveTimeout = 500;
            socket.SendTimeout = 500;
            socket.ReceiveBufferSize = BufferSize;
            socket.SendBufferSize = BufferSize;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                socket.IOControl(SioUdpConnreset, new byte[] {0}, null);
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
            return socket.IsBound;
        }

        public struct DataSent {
            public TaskCompletionSource<bool> TaskCompletion;
            public CancellationTokenRegistration Registration;
        }

        void Sent (object sender, SocketAsyncEventArgs e)
        {
            var tcs = (DataSent)(e.UserToken);
            using (tcs.Registration) {
                try {
                if (!tcs.TaskCompletion.Task.IsCanceled && !tcs.TaskCompletion.Task.IsFaulted)
                    tcs.TaskCompletion.TrySetResult (e.SocketError == SocketError.Success);
                } catch (Exception ex) {
                    Console.WriteLine (ex);
                }
            }
        }

        public struct DataReceived {
            public TaskCompletionSource<(EndPoint remote, byte [] data, int length)> TaskCompletion;
            public System.Threading.CancellationTokenRegistration Registration;
        }

        void Received (object sender, SocketAsyncEventArgs e)
        {
            DataReceived tcs = (DataReceived)(e.UserToken);
            using (tcs.Registration) {
                try {
                if (!tcs.TaskCompletion.Task.IsCanceled && !tcs.TaskCompletion.Task.IsFaulted)
                    tcs.TaskCompletion.TrySetResult ((e.RemoteEndPoint, e.Buffer, e.BytesTransferred));
                } catch (Exception ex) {
                    Console.WriteLine (ex);
                }
            }
        }

        public UDPSocket(string name = "UDPSocket")
        {
            _name = name;
            socketIP4 = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //socketIP6 = new Socket (AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            SetupSocket (socketIP4, reuseAddress: true);
            //SetupSocket (socketIP6, reuseAddress: true);
        }

        public bool Listen (int port)
        {
            var ep = new IPEndPoint (IPAddress.Any, port);
            bool result = Bind (socketIP4, ep);
            Console.WriteLine ($"{_name} is Listening on {ep} {result}");
            //var epV6 = new IPEndPoint (IPAddress.IPv6Any, port);
            //result &= Bind (socketIP6, epV6);
            return result;
        }

        public Task<(EndPoint remote, byte [] data, int length)> ReceiveFrom (EndPoint endPoint, System.Threading.CancellationToken token)
        {
            SocketAsyncEventArgs receiveAsyncArgs = new SocketAsyncEventArgs ();
            TaskCompletionSource<(EndPoint remote, byte [] data, int length)> receiveTcs = new TaskCompletionSource<(EndPoint remote, byte[] data, int length)> ();
            receiveAsyncArgs.RemoteEndPoint = endPoint;
            receiveAsyncArgs.Completed += Received;
            var buffer = pool.Rent (socketIP4.ReceiveBufferSize);
            receiveAsyncArgs.SetBuffer (buffer, 0, buffer.Length);
            var registation = token.Register (() => receiveTcs.TrySetCanceled ());
            receiveAsyncArgs.UserToken = new DataReceived { 
                TaskCompletion = receiveTcs,
                Registration = registation,
            };
            if (endPoint.AddressFamily == AddressFamily.InterNetwork && (!socketIP4.ReceiveFromAsync (receiveAsyncArgs))) {
               Received (this, receiveAsyncArgs);
            }
            return receiveTcs.Task;
        }

        public void ReturnBuffer (byte[] buffer){
            pool.Return (buffer);
        }

        public Task<bool> SendTo (EndPoint endPoint, byte[] data, System.Threading.CancellationToken token)
        {
            SocketAsyncEventArgs sendAsyncArgs = new SocketAsyncEventArgs ();
            TaskCompletionSource<bool> sendTcs = new TaskCompletionSource<bool> ();
            var registration = token.Register (() => sendTcs.TrySetCanceled ());
            sendAsyncArgs.RemoteEndPoint = endPoint;
            sendAsyncArgs.Completed += Sent;
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

        public void Dispose()
        {
            try {
                if (socketIP4 != null) {
                    socketIP4.Close ();
                }
            } catch {

            }
            // try {
            //     if (socketIP6 != null) {
            //         socketIP6.Close ();
            //     }
            // } catch {
                
            // }
        }
    }
}