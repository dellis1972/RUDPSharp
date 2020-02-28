using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class RUDPTests {

        MockUDPSocket clientSocket;
        MockUDPSocket serverSocket;
        [SetUp]
        public void Setup ()
        {
            clientSocket = new MockUDPSocket ("MockClient");
            serverSocket = new MockUDPSocket ("MockServer");
            serverSocket.Link (clientSocket);
        }

        [Test]
        public async Task TestMockSocketLink ()
        {
            serverSocket.Listen (8000);
            clientSocket.Listen (8001);

            var serverAny = new IPEndPoint(IPAddress.Any, 8000);
            var clientAny = new IPEndPoint(IPAddress.Any, 8001);


            var ping = Encoding.ASCII.GetBytes ("Ping");
            Assert.IsTrue (await clientSocket.SendTo (serverSocket.EndPoint, ping, default));
            var data = await serverSocket.ReceiveFrom (serverAny, default);
            Assert.AreEqual (clientSocket.EndPoint, data.remote);
            Assert.AreEqual (ping.Length, data.length);
            Assert.AreEqual (ping, data.data, $"({(string.Join (",", data.data))}) != ({(string.Join (",", ping))})");

            var pong = Encoding.ASCII.GetBytes ("Pong");
            Assert.IsTrue (await serverSocket.SendTo (clientSocket.EndPoint, pong, default));
            data = await clientSocket.ReceiveFrom (serverAny, default);
            Assert.AreEqual (serverSocket.EndPoint, data.remote);
            Assert.AreEqual (pong.Length, data.length);
            Assert.AreEqual (pong, data.data, $"({(string.Join (",", data.data))}) != ({(string.Join (",", pong))})");
        }

        [Test]
        public void CheckMessagesAreSent ()
        {
            // var rUDPServer = new RUDP<MockUDPSocket>(serverSocket);
            // rUDPServer.Start (0);
            // var rUDPClient = new RUDP<MockUDPSocket>(clientSocket);
            //rUDPClient.Start (0);
        }
    }
}