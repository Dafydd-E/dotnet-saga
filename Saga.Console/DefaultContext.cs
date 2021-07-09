using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Saga.Eventing;

namespace Saga.Console
{
    public class DefaultContext : IAsyncDisposable, IEventingSagaContext<Event>
    {
        public IServiceScope ServiceScope { get; }

        public Event Event { get; }

        public DefaultContext(IServiceScope serviceScope)
        {
            this.ServiceScope = serviceScope;
        }

        public DefaultContext(IServiceScope serviceScope, Event @event) : this(serviceScope)
        {
            this.Event = @event;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.Run(() => this.ServiceScope.Dispose()));
        }
    }
}