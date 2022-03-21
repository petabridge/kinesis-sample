using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using Akka.DependencyInjection;
using Amazon.Kinesis;
using Akka.Streams.Kinesis;
using Akka.Streams;
using Amazon.Runtime;
using Amazon;
using Akka.Streams.Dsl;
using System.Text.Json;
using Shared;
using System.Text;
using Amazon.Kinesis.Model;
using System.Linq;

namespace KinesisSample
{
    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private ActorSystem ClusterSystem;
        private readonly IServiceProvider _serviceProvider;

        private readonly IHostApplicationLifetime _applicationLifetime;
        private string _streamName;
        Func<IAmazonKinesis> _clientFactory;
        private IMaterializer _materializer;
        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var clusterSeeds = Environment.GetEnvironmentVariable("CLUSTER_SEEDS")?.Trim();
            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("app.conf"));
            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes").ToList();
            if (!string.IsNullOrEmpty(clusterSeeds))
            {
                var tempSeeds = clusterSeeds.Trim('[', ']').Split(',').ToList();
                if (tempSeeds.Any())
                {
                    seeds = tempSeeds;
                }
            }
            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + @"""" + seed + @""", ");
            injectedClusterConfigString += "]";
            var config = clusterConfig
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString))
                .BootstrapFromDocker();
            var bootstrap = BootstrapSetup.Create()
               .WithConfig(config) // load HOCON
               .WithActorRefProvider(ProviderSelection.Cluster.Instance); // launch Akka.Cluster

            // N.B. `WithActorRefProvider` isn't actually needed here - the HOCON file already specifies Akka.Cluster

            // enable DI support inside this ActorSystem, if needed
            var diSetup = DependencyResolverSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            ClusterSystem = ActorSystem.Create("FireAlert", actorSystemSetup);

            // use the ServiceProvider ActorSystem Extension to start DI'd actors
            var sp = DependencyResolver.For(ClusterSystem);
            // add a continuation task that will guarantee 
            // shutdown of application if ActorSystem terminates first
            ClusterSystem.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            });

            #region Kinesis
            var vrs = Environment.GetEnvironmentVariables();
            _clientFactory = () => new AmazonKinesisClient(
                new BasicAWSCredentials(vrs["accessKey"]?.ToString(), vrs["accessSecret"]?.ToString()),
                RegionEndpoint.USEast1);
            _materializer = ClusterSystem.Materializer();
            _streamName = vrs["streamName"]?.ToString();
            var describeRequest = new DescribeStreamRequest
            {
                StreamName = _streamName,
            };
            var describeResponse = await _clientFactory.Invoke().DescribeStreamAsync(describeRequest);
            var shards = describeResponse.StreamDescription.Shards;
            var aggregator = ClusterSystem.ActorOf(AggregatorActor.Prop(ClusterSystem));
            foreach(var shard in shards)
            {

                ClusterSystem.ActorOf(Props.Create(() => new ShardActor(aggregator, shard, _clientFactory, _streamName, _materializer)));
            }
            #endregion
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await CoordinatedShutdown.Get(ClusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }

}
