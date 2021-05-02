using System.Threading.Tasks;

namespace Saga
{
    public interface ISaga<in TEvent, in TContext>
    {
        public bool Retry { get; }
        
        Task Then(TEvent @event, TContext context);
    }
}