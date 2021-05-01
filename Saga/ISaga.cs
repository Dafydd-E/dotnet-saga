using System.Threading.Tasks;

namespace Saga
{
    public interface ISaga<in TEvent, in TState>
    {
        public bool Retry { get; }
        
        Task Then(TEvent @event, TState state);

        Task Rollback(TEvent @event, TState state);
    }
}