using Akka.Cluster.Sharding;
namespace Shared
{
    public sealed class FireAlertMessageExtractor : HashCodeMessageExtractor
    {
        public FireAlertMessageExtractor()
            : base(Constants.MaximumNumberOfShards)
        { }

        public override string EntityId(object message) => (message as ShardEnvelope)?.EntityId;
        public override object EntityMessage(object message) => (message as ShardEnvelope)?.Message;
    }
}