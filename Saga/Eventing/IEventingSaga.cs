using System;

namespace Saga.Eventing
{
    public interface IEventingSaga<TContext, TEvent> where TContext : IEventingSagaContext<TEvent>, IAsyncDisposable
    {
        public EventHandler<TContext> Run { get; }

        public EventHandler<TContext> Rollback { get; }
    }
}