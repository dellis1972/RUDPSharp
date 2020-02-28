using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class MockUDPSocket : UDPSocket {

        ConcurrentQueue<(EndPoint endPoint, byte[] data)> outgoing = new ConcurrentQueue<(EndPoint endPoint, byte[] data)> ();
        ConcurrentQueue<(EndPoint endPoint, byte[] data)> incoming = new ConcurrentQueue<(EndPoint endPoint, byte[] data)> ();

        MockUDPSocket link;
        bool listening = false;
        EndPoint endPoint;

        public new EndPoint EndPoint {
            get { return endPoint; }
        }

        public MockUDPSocket(string name = "UDPSocket")
        {

        }

        public override void Initialize()
        {

        }

        public void Link (MockUDPSocket socket){
            socket.link = this;
            link = socket;
        }

        public override bool Listen (int port)
        {
            endPoint = new IPEndPoint (IPAddress.Loopback, port);
            listening = true;
            return listening;
        }

        public async override Task<(EndPoint remote, byte [] data, int length)> ReceiveFrom (EndPoint endPoint, System.Threading.CancellationToken token)
        {
            if (listening && incoming.TryDequeue (out (EndPoint endPoint, byte[] data) packet)) {
                return (packet.endPoint, packet.data, packet.data.Length);
            }
            return (new IPEndPoint (0,0), new byte[0], 0);
        }

        public override void ReturnBuffer (byte[] buffer){
        }

        public async override Task<bool> SendTo (EndPoint endPoint, byte[] data, System.Threading.CancellationToken token)
        {
            link.incoming.Enqueue ((EndPoint, data));
            return true;
        }
    }
}