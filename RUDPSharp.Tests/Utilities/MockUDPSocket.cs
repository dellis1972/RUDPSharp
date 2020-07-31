using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class MockUDPSocket : UDPSocket {

        MockUDPSocket link;
        bool listening = false;
        EndPoint endPoint;

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

        protected override EndPoint GetEndPoint ()
        {
            return endPoint;
        }

        public override void ReturnBuffer (byte[] buffer){
        }

        public async override Task<bool> SendTo (EndPoint endPoint, byte[] data, System.Threading.CancellationToken token)
        {
            return link.RecievedPackets.TryAdd ((EndPoint, data));
        }
    }
}