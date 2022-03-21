using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Akka.Streams;

namespace KinesisProducer
{
    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private readonly ActorSystem _system;
        private readonly IMaterializer _materializer;
        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
            var vrs = Environment.GetEnvironmentVariables();
            var sensors = vrs["sensors"]?.ToString().Split(";").ToList();
            _system = ActorSystem.Create(vrs["ACTORSYSTEM"]?.ToString());
            _materializer = _system.Materializer();
            foreach(var sensor in sensors)
            {
                _system.ActorOf(SensorActor.Prop(sensor, _materializer));
            }

            _system.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await _system.Terminate();
        }
    }

}
