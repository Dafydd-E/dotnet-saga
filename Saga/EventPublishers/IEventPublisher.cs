using System;
using System.Threading.Tasks;

namespace Saga.EventPublishers
{
    public interface IEventPublisher
    {
        Task<TResult> Publish<TEvent, TContext, TResult>(TEvent @event) where TContext : IDisposable; 
    }
}