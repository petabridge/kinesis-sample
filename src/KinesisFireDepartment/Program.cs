// See https://aka.ms/new-console-template for more information
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using KinesisFireDepartment.Actors;
using KinesisFireDepartment.Messages;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Shared;

var cords = Environment.GetEnvironmentVariable("coordinate").Split(",");
var name = Environment.GetEnvironmentVariable("name");
var city = Environment.GetEnvironmentVariable("city");
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

// start ActorSystem
var _system = ActorSystem.Create(Environment.GetEnvironmentVariable("ACTORSYSTEM"), bootstrap);

// start Petabridge.Cmd (https://cmd.petabridge.com/)
var pbm = PetabridgeCmd.Get(_system);
pbm.RegisterCommandPalette(ClusterCommands.Instance);
pbm.RegisterCommandPalette(new RemoteCommands());
pbm.Start(); // begin listening for PBM management commands

var sharding = ClusterSharding.Get(_system);
var settings = ClusterShardingSettings
    .Create(_system)
    .WithRole("node");

var shardRegion = sharding.Start(
    typeName: "SensorData",
    entityProps: Props.Create<DepartmentActor>(),
    settings: settings,
    messageExtractor: new FireAlertMessageExtractor());

var env = new ShardEnvelope("SensorData", new DepartmentOnline(name, city, double.Parse(cords[0]), double.Parse(cords[1])));
shardRegion.Tell(env);

Console.CancelKeyPress += async (sender, eventArgs) =>
{
    await CoordinatedShutdown.Get(_system).Run(CoordinatedShutdown.ClrExitReason.Instance); 
};

await _system.WhenTerminated;
