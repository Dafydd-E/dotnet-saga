using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saga.Eventing;

namespace Saga.Console
{
    public class EventingSaga : IEventingSaga<DefaultContext, Event>
    {

        public EventingSaga(ILogger<EventingSaga> logger)
        {
            this.Run += (object sender, DefaultContext context) => logger.LogInformation("I am running");
            this.Rollback += (object sender, DefaultContext context) => logger.LogInformation("I am rolling back");
        }

        public EventHandler<DefaultContext> Run { get; }

        public EventHandler<DefaultContext> Rollback { get; }
    }

    public class ExceptionEventingSaga : IEventingSaga<DefaultContext, Event>
    {
        public ExceptionEventingSaga()
        {
            this.Run += (object sender, DefaultContext context) => throw new Exception("I have errored");
        }

        public EventHandler<DefaultContext> Run { get; }

        public EventHandler<DefaultContext> Rollback { get; }
    }
}