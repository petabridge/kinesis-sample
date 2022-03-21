using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kinesis;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Shared;
using System;
using System.Text;
using System.Text.Json;

namespace KinesisSample
{
    public class ShardActor: ReceiveActor
    {
        private IActorRef _aggregator;
        private readonly ILoggingAdapter _log;
        public ShardActor(IActorRef aggregator, Shard shard, Func<IAmazonKinesis> clientFactory, string streamName, IMaterializer materializer)
        {
            _log = Context.GetLogger();
            _aggregator = aggregator;  
            var shardSetting = new ShardSettings(streamName, shard.ShardId, ShardIteratorType.AT_TIMESTAMP,
                   TimeSpan.FromSeconds(1), 10000, atTimestamp: DateTime.MinValue);
            var graph = KinesisSource.Basic(shardSetting, clientFactory)
                .Select(x => x)
                .To(Sink.ActorRef<Record>(Self, "done"))
                .Run(materializer);
            Receive<Record>(s => 
            {
                var sdata = JsonSerializer.Deserialize<SensorData>(Encoding.UTF8.GetString(s.Data.ToArray()));
                _aggregator.Tell(sdata);
            });
            Receive<string>(s => 
            {
                _log.Info(s);
            });
        }
    }
}
