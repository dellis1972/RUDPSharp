using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class MockUDPSocket : UDPSocket {

        List<MockUDPSocket> links = new List<MockUDPSocket> ();
        bool listening = false;
        EndPoint endPoint;

        public MockUDPSocket(string name = "UDPSocket")
        {
        }

        public override void Initialize()
        {

        }

        public void Link (MockUDPSocket socket){
            socket.links.Add (this);
            links.Add (socket);
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

        public override Task<bool> SendTo (EndPoint endPoint, byte[] data, System.Threading.CancellationToken token)
        {
            foreach (var link in links) {
                if (link.endPoint.Equals (endPoint)) {
                    return Task.FromResult(link.ReceivedPackets.TryAdd ((EndPoint, data)));
                }
            }
            return Task.FromResult (false);
        }
    }
}