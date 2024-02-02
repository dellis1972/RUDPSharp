using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RUDPSharp;

namespace RUDPSharp.Tests
{
    public class RUDPTests {

        MockUDPSocket clientSocket;
        MockUDPSocket serverSocket;

        IPEndPoint serverAny = new IPEndPoint(IPAddress.Loopback, 8000);
        IPEndPoint clientAny = new IPEndPoint(IPAddress.Loopback, 8001);

        protected static void WaitFor(int milliseconds)
		{
			var pause = new ManualResetEvent(false);
			pause.WaitOne(milliseconds);
		}
        
        [SetUp]
        public void Setup ()
        {
            clientSocket = new MockUDPSocket ("MockClient");
            serverSocket = new MockUDPSocket ("MockServer");
            serverSocket.Link (clientSocket);
        }

        [Test]
        public void TestActualSockets ()
        {
            RUDP<UDPSocket> s1 = new RUDP<UDPSocket>(new UDPSocket());
            RUDP<UDPSocket> s2 = new RUDP<UDPSocket>(new UDPSocket());
            var wait = new ManualResetEvent (false);

            s2.ConnectionRequested += (EndPoint e, byte[] data) => {
                Console.WriteLine($"{e} Connected. {Encoding.ASCII.GetString(data)}");
                wait.Set ();
                return true;
            };
            s2.DataReceived = (EndPoint e, byte[] data) => {
                wait.Set ();
                return true;
            };
            s1.Start(4545);
            s2.Start(5454);
            wait.Reset ();
            s1.Connect("127.0.0.1", 5454);
            wait.WaitOne (5000);
            wait.Reset ();
            s1.SendToAll(Channel.ReliableInOrder, new byte[1024]);

            wait.WaitOne (5000);

            Assert.IsTrue (s1.Disconnect ());
            Assert.IsTrue (s2.Disconnect ());
        }

        // [Test]
        // public async Task ClientExample ()
        // {
        //     using (var rUDPClient = new RUDP<MockUDPSocket>(clientSocket)) {
        //         rUDPClient.Start (8001);
        //         bool result = await rUDPClient.ConnectAsync (serverAny.Address.ToString (), 8000);
        //         if (!result)
        //             Assert.Fail ();
        //         var ping = Encoding.ASCII.GetBytes ("Ping");
        //         rUDPClient.SendToAll (Channel.None, ping);
        //         rUDPClient.SendTo (serverSocket.EndPoint, Channel.None, ping);
        //     }
        // }

        [Test]
        public void TestClientCanConnectAndDisconnect ()
        {
             using (var rUDPServer = new RUDP<MockUDPSocket>(serverSocket)) {
                using (var rUDPClient = new RUDP<MockUDPSocket>(clientSocket)) {
                    var serverWait = new ManualResetEvent (false);
                    rUDPServer.ConnectionRequested += (EndPoint e, byte[] data) => {
                        serverWait.Set ();
                        return true;
                    };
                    rUDPServer.Start (8000);
                    rUDPClient.Start (8001);
                    rUDPClient.Connect (serverAny.Address.ToString (), 8000);
                    serverWait.WaitOne (500);
                    serverWait.Reset ();
                    Assert.AreEqual (1, rUDPServer.Remotes.Count);
                    Assert.AreEqual (1, rUDPClient.Remotes.Count);

                    rUDPClient.Disconnect ();
                    Thread.Sleep (100);
                    Assert.AreEqual (0, rUDPServer.Remotes.Count);
                    Assert.AreEqual (0, rUDPClient.Remotes.Count);
                    rUDPServer.Disconnect ();
                }
             }
        }

        [Test]
        public async Task TestMockSocketLink ()
        {
            serverSocket.Initialize ();
            serverSocket.Listen (serverAny.Port);
            clientSocket.Initialize ();
            clientSocket.Listen (clientAny.Port);
            var ping = Encoding.ASCII.GetBytes ("Ping");
            Assert.IsTrue (await clientSocket.SendTo (serverSocket.EndPoint, ping, default));
            
            var data = serverSocket.ReceivedPackets.Take ();
            Assert.AreEqual (clientSocket.EndPoint, data.remote);
            Assert.AreEqual (ping.Length, data.data.Length);
            Assert.AreEqual (ping, data.data, $"({(string.Join (",", data.data))}) != ({(string.Join (",", ping))})");

            var pong = Encoding.ASCII.GetBytes ("Pong");
            Assert.IsTrue (await serverSocket.SendTo (clientSocket.EndPoint, pong, default));
            data = clientSocket.ReceivedPackets.Take ();
            Assert.AreEqual (serverSocket.EndPoint, data.remote);
            Assert.AreEqual (pong.Length, data.data.Length);
            Assert.AreEqual (pong, data.data, $"({(string.Join (",", data.data))}) != ({(string.Join (",", pong))})");
            serverSocket.Dispose ();
            clientSocket.Dispose ();
        }

        [Test]
        public void CheckMessagesAreSent ()
        {
            using (var rUDPServer = new RUDP<MockUDPSocket>(serverSocket)) {
                using (var rUDPClient = new RUDP<MockUDPSocket>(clientSocket)) {
                    EndPoint remote = null;
                    byte[] dataReceived = null;
                    var clientWait = new ManualResetEvent (false);
                    var serverWait = new ManualResetEvent (false);
                    rUDPServer.ConnectionRequested += (EndPoint e, byte[] data) => {
                        serverWait.Set ();
                        return true;
                    };
                    rUDPServer.DataReceived = (EndPoint e, byte[] data) => {
                        serverWait.Set ();
                        remote = e;
                        dataReceived = data;
                        return true;
                    };
                    rUDPClient.ConnectionRequested += (EndPoint e, byte[] data) => {
                        clientWait.Set ();
                        return true;
                    };
                    rUDPClient.DataReceived = (EndPoint e, byte [] data) => {
                        clientWait.Set ();
                        remote = e;
                        dataReceived = data;
                        return true;
                    };
                    rUDPServer.Start (8000);
                    rUDPClient.Start (8001);
                    Assert.IsTrue (rUDPClient.Connect (serverAny.Address.ToString (), 8000));
                    clientWait.WaitOne (500);
                    serverWait.WaitOne (500);
                    serverWait.Reset ();
                    clientWait.Reset ();
                    Assert.AreEqual(1, rUDPServer.Remotes.Count);
                    Assert.AreEqual (rUDPClient.EndPoint, rUDPServer.Remotes.First ());

                    Assert.AreEqual(1, rUDPClient.Remotes.Count);
                    Assert.AreEqual (rUDPServer.EndPoint, rUDPClient.Remotes.First ());

                    byte[] message = Encoding.ASCII.GetBytes ("Ping");
                    Assert.IsTrue (rUDPClient.SendTo (rUDPServer.EndPoint, Channel.None, message));
                    clientWait.WaitOne (5000);

                    Assert.AreEqual (message, dataReceived, $"({(string.Join (",", dataReceived ?? Array.Empty<byte> ()))}) != ({(string.Join (",", message))})");
                    Assert.AreEqual (rUDPClient.EndPoint, remote);

                    message = Encoding.ASCII.GetBytes ("Pong");
                    dataReceived = null;
                    remote = null;
                    serverWait.Reset ();
                    Assert.IsTrue (rUDPServer.SendToAll (Channel.None, message));

                    serverWait.WaitOne (5000);

                    Assert.AreEqual (message, dataReceived, $"({(string.Join (",", dataReceived ?? Array.Empty<byte> ()))}) != ({(string.Join (",", message))})");
                    Assert.AreEqual (rUDPServer.EndPoint, remote);

                    Assert.IsTrue (rUDPClient.Disconnect ());
                    Thread.Sleep (100);
                    Assert.AreEqual (0, rUDPServer.Remotes.Count);
                    Assert.AreEqual (0, rUDPClient.Remotes.Count);
                }
            }
        }

        [Test]
        public void TestLargePacketIsDelivered ()
        {
            var rnd = new Random ();
            using (var rUDPServer = new RUDP<MockUDPSocket>(serverSocket)) {
                using (var rUDPClient = new RUDP<MockUDPSocket>(clientSocket)) {
                    EndPoint remote = null;
                    byte[] dataReceived = null;
                    var wait = new ManualResetEvent (false);
                    var serverWait = new ManualResetEvent (false);
                    rUDPServer.ConnectionRequested += (EndPoint e, byte[] data) => {
                        serverWait.Set ();
                        return true;
                    };
                    rUDPServer.DataReceived = (EndPoint e, byte[] data) => {
                        wait.Set ();
                        remote = e;
                        dataReceived = data;
                        return true;
                    };
                    rUDPClient.ConnectionRequested += (EndPoint e, byte[] data) => {
                        wait.Set ();
                        return true;
                    };
                    rUDPClient.DataReceived = (EndPoint e, byte [] data) => {
                        wait.Set ();
                        remote = e;
                        dataReceived = data;
                        return true;
                    };
                    rUDPServer.Start (8000);
                    rUDPClient.Start (8001);
                    Assert.IsTrue (rUDPClient.Connect (serverAny.Address.ToString (), 8000));
                    wait.WaitOne (500);
                    serverWait.WaitOne (500);
                    wait.Reset ();
                    serverWait.Reset ();
                    Assert.AreEqual(1, rUDPServer.Remotes.Count);
                    Assert.AreEqual (rUDPClient.EndPoint, rUDPServer.Remotes.First ());

                    Assert.AreEqual(1, rUDPClient.Remotes.Count);
                    Assert.AreEqual (rUDPServer.EndPoint, rUDPClient.Remotes.First ());

                    byte[] message = new byte[1024];
                    rnd.NextBytes (message);
                    Assert.IsTrue (rUDPClient.SendTo (rUDPServer.EndPoint, Channel.None, message));
                    wait.WaitOne (5000);

                    Assert.AreEqual (message, dataReceived, $"({(string.Join (",", dataReceived ?? Array.Empty<byte> ()))}) != ({(string.Join (",", message))})");
                    Assert.AreEqual (rUDPClient.EndPoint, remote);

                    message = new byte[1024];
                    rnd.NextBytes (message);
                    dataReceived = null;
                    remote = null;
                    serverWait.Reset ();
                    Assert.IsTrue (rUDPServer.SendToAll (Channel.None, message));

                    serverWait.WaitOne (5000);

                    Assert.AreEqual (message, dataReceived, $"({(string.Join (",", dataReceived ?? Array.Empty<byte> ()))}) != ({(string.Join (",", message))})");
                    Assert.AreEqual (rUDPServer.EndPoint, remote);

                    Assert.IsTrue (rUDPClient.Disconnect ());
                    Thread.Sleep (100);
                    Assert.AreEqual (0, rUDPServer.Remotes.Count);
                    Assert.AreEqual (0, rUDPClient.Remotes.Count);
                }
            }
        }
    }
}