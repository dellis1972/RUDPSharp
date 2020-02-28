using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RUDPSharp;

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
}
