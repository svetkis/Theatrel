using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using theatrel.TLBot.Interfaces;

namespace theatrel.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ITLBotProcessor _tLBotProcessor;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Bootstrapper.Start();
            await base.StartAsync(cancellationToken);

            Trace.Listeners.Add(new Trace2StdoutLogger());

            Trace.TraceInformation("Worker.StartAsync");

            _tLBotProcessor = Bootstrapper.Resolve<ITLBotProcessor>();
            _tLBotProcessor.Start(Bootstrapper.Resolve<ITLBotService>(), cancellationToken);

            return;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Worker.StopAsync");

            _tLBotProcessor.Stop();
            Bootstrapper.Stop();

            await base.StopAsync(cancellationToken);
            return;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
