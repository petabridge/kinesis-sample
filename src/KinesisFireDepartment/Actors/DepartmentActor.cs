using Akka.Actor;
using Akka.Event;
using KinesisFireDepartment.Messages;
using Shared;
using System.Text.Json;

namespace KinesisFireDepartment.Actors
{
    public class DepartmentActor : ReceiveActor
    {
        private readonly List<DepartmentOnline> _departmentsCoordinates = new List<DepartmentOnline>();
        private readonly ILoggingAdapter _log;
        private readonly string _name;  
        public DepartmentActor()
        {
            _name = Environment.GetEnvironmentVariable("name"); 
            _log = Context.GetLogger();
            Receive<FireAlert>(f =>
            {
                _log.Info($"{_name} RECEIVED =>>>> {f}");
            });
            Receive<DepartmentOnline>(f =>
            {
                if (!_departmentsCoordinates.Contains(f))
                {
                    _departmentsCoordinates.Add(f);
                    _log.Info($"Department added: {f}");
                }
                else
                    _log.Info($"Department already added: {f}");
            });
        }
    }
}
