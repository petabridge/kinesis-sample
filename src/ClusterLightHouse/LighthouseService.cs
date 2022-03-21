using System.Threading.Tasks;
using Akka.Actor;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

namespace ClusterLightHouse
{
    public class LighthouseService
    {
        private readonly string _ipAddress;
        private readonly int? _port;
        private readonly string _actorSystemName;

        public ActorSystem LighthouseSystem;

        public LighthouseService(string ipAddress = null, int? port = null, string actorSystemName = null)
        {
            _ipAddress = ipAddress;
            _port = port;
            _actorSystemName = actorSystemName;
        }

        public void Start()
        {
            LighthouseSystem = LighthouseHostFactory.LaunchLighthouse(_ipAddress, _port, _actorSystemName);
            var pbm = PetabridgeCmd.Get(LighthouseSystem);
            pbm.RegisterCommandPalette(ClusterCommands.Instance); // enable cluster management commands
            pbm.Start();
        }

        /// <summary>
        /// Task completes once the Lighthouse <see cref="ActorSystem"/> has terminated.
        /// </summary>
        /// <remarks>
        /// Doesn't actually invoke termination. Need to call <see cref="StopAsync"/> for that.
        /// </remarks>
        public Task TerminationHandle => LighthouseSystem.WhenTerminated;

        public async Task StopAsync()
        {
            await CoordinatedShutdown
                .Get(LighthouseSystem)
                .Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}
