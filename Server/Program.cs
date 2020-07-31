using System;
using System.Text;
using System.Threading;
using RUDPSharp;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var server = new RUDP<UDPSocket> (new UDPSocket ("ServerSocket"))) {
                server.ConnetionRequested += (e, d) => {
                    Console.WriteLine ($"{e} Connected. {Encoding.ASCII.GetString (d)}");
                    return true;
                };
                server.DataReceived += (e, d) => {
                    string message = Encoding.UTF8.GetString (d);
                    Console.WriteLine ($"{e}: {message} ");
                    server.SendTo (e, Channel.ReliableInOrder, d);
                    return true;
                };
                server.Disconnected += (e) => {
                    Console.WriteLine ($"{e} Disconnected.");
                };
                server.Start (8001);
                Console.WriteLine ("Waiting...");
                bool done = false;
                while (!done) {
                    if (Console.KeyAvailable && Console.ReadKey ().Key == ConsoleKey.Q) {
                        break;
                    }
                    Thread.Sleep(10);
                }
                Console.WriteLine ("Shutting Down");
                server.Disconnect ();
            }
        }
    }
}
