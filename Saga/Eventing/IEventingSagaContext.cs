namespace Saga.Eventing
{
    public interface IEventingSagaContext<TEvent>
    {
        public TEvent Event { get; }
    }
}