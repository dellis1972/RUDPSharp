using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RUDPSharp;
using System.Text;

namespace RUDPSharp.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var server = new RUDP<UDPSocket> (new UDPSocket ("ServerSocket"))) {
                server.ConnectionRequested += (e, d) => {
                    _logger.LogInformation ( $"{e} Connected. {Encoding.ASCII.GetString (d)}");
                    return true;
                };
                server.DataReceived += (e, d) => {
                    string message = Encoding.UTF8.GetString (d);
                    _logger.LogInformation ($"{e}: {message} ");
                    server.SendTo (e, Channel.ReliableInOrder, d);
                    return true;
                };
                server.Start (8001);
                _logger.LogInformation ("Running.");
                while (!stoppingToken.IsCancellationRequested) {
                    Thread.Sleep(10);
                }
                _logger.LogInformation ("Shutting Down");
                server.Disconnect ();
            }
            return Task.CompletedTask;
        }
    }
}
