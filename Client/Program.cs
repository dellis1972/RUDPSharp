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
using System.Threading;

namespace Client
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Console.WriteLine ("Enter Host IP");
            string remoteIp = Console.ReadLine ();
            IPAddress remoteIpAddress = IPAddress.Parse (remoteIp);
            Console.WriteLine ("Enter Host Port");
            string remotePort = Console.ReadLine ();
            int port = int.Parse (remotePort);

            using (var client = new RUDP<UDPSocket> (new UDPSocket ("ClientSocket"))) {
                client.ConnetionRequested += (e, d) => {
                    Console.WriteLine ($"{e} Connected. {Encoding.ASCII.GetString (d)}");
                    return true;
                };
                client.DataReceived += (e, d) => {
                    Console.WriteLine ($"{e}: {Encoding.UTF8.GetString (d)} ");
                    return true;
                };
                client.Start (8000);
                Console.WriteLine ("Connecting...");
                client.Connect (remoteIpAddress.ToString (), port);
                EndPoint ep = new IPEndPoint (remoteIpAddress, port);
                Console.WriteLine ("Connected.");

                bool done = false;
                string message = string.Empty;
                while (!done) {
                    if (Console.KeyAvailable) {
                        var info =  Console.ReadKey ();
                        if (info.Key == ConsoleKey.Q && info.Modifiers.HasFlag (ConsoleModifiers.Control))
                            break;
                        if (info.Key == ConsoleKey.Enter) {
                            client.SendTo (ep, Channel.ReliableInOrder, Encoding.UTF8.GetBytes (message));
                            message = string.Empty;
                            continue;
                        }
                        if (info.Key == ConsoleKey.P && info.Modifiers.HasFlag (ConsoleModifiers.Control)) {
                            client.Ping ();
                            continue;
                        }
                        message += info.KeyChar;
                    }
                    Thread.Sleep(10);
                }
                Console.WriteLine ("Shutting Down");
                client.Disconnect ();
            }
        //     int serverPort = 8002;
        //     int clientPort = 8003;
        //     bool done = false;
        //     List<Task> tasks = new List<Task> ();
        //     System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource ();
        //     using (var server = new UDPSocket ("Server")) {
        //         server.Initialize ();
        //         server.Listen (serverPort);
        //         tasks.Add (Task.Run (async () => {
        //             int i = 50000;
        //             try {
        //                 while (!done) {
        //                     var data = await server.ReceiveFrom (new IPEndPoint(IPAddress.Any, serverPort), cts.Token);
        //                     if (data.length > 0) {
        //                         Console.WriteLine ( $"SERVER: Got {Encoding.ASCII.GetString (data.data)} from {data.remote}");
        //                         server.ReturnBuffer (data.data);
        //                         await server.SendTo (data.remote, Encoding.ASCII.GetBytes ($"ACK{i++}"), cts.Token);
        //                     }
        //                 }
        //             } catch (Exception ex) {
        //                 Console.WriteLine(ex);
        //             } finally {
        //                 Console.WriteLine ($"Server {i-50000}");
        //             }
        //         }));
        //         tasks.Add (Task.Run ( async () => {
        //             int i = 20000;
        //             try {
        //                 using (var socket = new UDPSocket ($"Client 1")) {
        //                     socket.Initialize ();
        //                     socket.Listen (clientPort+1);
        //                     var local = new IPEndPoint (IPAddress.Parse ("127.0.0.1"), serverPort);
                            
        //                     int c = 1000;
                            
        //                     while (!done) {
        //                         var data = await socket.ReceiveFrom (local, cts.Token);
        //                         if (data.length > 0) {
        //                             Console.WriteLine ( $"CLIENT: Got {Encoding.ASCII.GetString (data.data)} from {data.remote}");
        //                             socket.ReturnBuffer (data.data);
        //                             //await socket.SendTo (data.remote, Encoding.ASCII.GetBytes ($"ACK{i++}"), cts.Token);
        //                         }
        //                         c--;
        //                         if (c < 0)
        //                             await socket.SendTo (local, Encoding.ASCII.GetBytes ("Hello"), cts.Token);
        //                     }
        //                 }
        //             } catch (Exception ex) {
        //                 Console.WriteLine(ex);
        //             } finally {
        //                 Console.WriteLine ($"Client 1 {i-20000}");
        //             }
        //         }));

        //         TimeSpan ts = TimeSpan.FromSeconds (5);
        //         while (!done) {
        //             System.Threading.Thread.Sleep (10);
        //             ts = ts.Add (-TimeSpan.FromMilliseconds (10));
        //             if (ts.TotalMilliseconds < 0) {
        //                 done = true;
        //                 cts.Cancel ();
        //                 System.Threading.Thread.Sleep(100);
        //                 try {
        //                 Task.WaitAll (tasks.ToArray ());
        //                 } catch {
        //                     // ignore we will get a cancelled error
        //                 }
        //                 Console.WriteLine ("Done");
        //             }
        //         }
        //     }
        }
    }
}
