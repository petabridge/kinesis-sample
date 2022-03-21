using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Shared;
using System;

namespace KinesisSample
{
    /// <summary>
    /// Receives messages from individual shard actor and publish it to the cluster via ClusterProxy
    /// </summary>
    public sealed class AggregatorActor:ReceiveActor
    {
        private readonly IActorRef _clusterProxy;
        private readonly int _alertThreshold;
        private readonly ILoggingAdapter _log;
        public AggregatorActor(ActorSystem system)
        {
            _log = Context.GetLogger();
            _alertThreshold = int.Parse(Environment.GetEnvironmentVariable("threshold"));
            var sharding = ClusterSharding.Get(system);

            _clusterProxy = sharding.StartProxy(
                typeName: "SensorData",
                role: "node",
                messageExtractor: new FireAlertMessageExtractor());
            Receive<SensorData>(sd =>
            {
                var sdata = sd;
                if(sdata.Reading > _alertThreshold)
                {
                    var alert = new FireAlert(sd.Coordinate, sd.Reading, Level(sd.Reading));
                    _clusterProxy.Tell(new ShardEnvelope("SensorData", alert));
                    _log.Info($"RECEIVED: READING `{sdata.Reading}` from {sdata.SensorName} location: {sdata.Coordinate.Latitude},{sdata.Coordinate.Longitude}");
                }
                
            });
        }
        public static Props Prop(ActorSystem actorSystem)
        {
            return Props.Create(()=> new AggregatorActor(actorSystem)); 
        }
        private FireAlertType Level(int reading)
        {
            if (reading < (reading * 0.1))
                return FireAlertType.Normal;

            if (reading < (reading * 0.20))
                return FireAlertType.Normal;

            if (reading < (reading * 0.50))
                return FireAlertType.High;

            if (reading < (reading * 0.75))
                return FireAlertType.VeryHigh;

            return FireAlertType.ExtremeHigh;
        }
    }
}
