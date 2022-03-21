using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kinesis;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Shared;
using System.Text;
using System.Text.Json;

namespace KinesisProducer
{
    public class SensorActor:ReceiveActor
    {
        private ICancelable _job;
        private readonly SensorSetting _setting;
        private readonly Random _random;
        private readonly Func<IAmazonKinesis> _clientFactory;
        private IActorRef _source;
        private readonly ILoggingAdapter _log;
        public SensorActor(string sensor, IMaterializer materializer)
        {
            _log = Context.GetLogger();
            _clientFactory = () => new AmazonKinesisClient(
                new BasicAWSCredentials(Environment.GetEnvironmentVariable("accessKey"),
                Environment.GetEnvironmentVariable("accessSecret")),
                RegionEndpoint.USEast1);

            _random = new Random();
            var sensorInfo = sensor.Split(",");
            _setting = new SensorSetting
            {
                SensorName = sensorInfo[2],
                Latitude = double.Parse(sensorInfo[0]),
                Longitude = double.Parse(sensorInfo[1]),
            };
            _source = Source.ActorRef<SensorData>(1000, OverflowStrategy.Fail)
             .Select(data => new PutRecordsRequestEntry
             {
                 PartitionKey = _random.Next().ToString(),
                 Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)))
             })
             .Via(KinesisFlow.Create(Environment.GetEnvironmentVariable("streamName"), KinesisFlowSettings.Default, _clientFactory))
             .To(Sink.ActorRef<PutRecordsResultEntry>(Self, "done"))
             .Run(materializer);

            _job = Context.System.Scheduler.Advanced
                .ScheduleRepeatedlyCancelable(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1),
                () =>
                {
                    var sensorData = new SensorData
                    {
                        SensorName = _setting.SensorName,
                        SensorId = _setting.SensorId,
                        Coordinate = new Coordinate
                        {
                            Latitude = _setting.Latitude,
                            Longitude = _setting.Longitude
                        },
                        Reading = _random.Next(75, 1000)
                    };
                    _source.Tell(sensorData);
                });
            Receive<PutRecordsResultEntry>(re =>
            {
                _log.Info(JsonSerializer.Serialize(re));
            });
        }
        public static Props Prop(string sensor, IMaterializer materializer)
        {
            return Props.Create(()=> new SensorActor(sensor, materializer));
        }
        protected override void PreStart()
        {
            base.PreStart();
        }
        protected override void PostStop()
        {
            _job?.Cancel();
            base.PostStop();
        }
    }
}
