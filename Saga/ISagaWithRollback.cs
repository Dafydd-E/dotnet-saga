using System.Threading.Tasks;

namespace Saga
{
    public interface ISagaWithRollback<in TEvent, in TContext> : ISaga<TEvent, TContext>
    {
        Task Rollback(TEvent @event, TContext context);
    }
}