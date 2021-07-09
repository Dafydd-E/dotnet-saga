using System;

namespace Saga
{
    public interface IDependencyProvider<TContext> where TContext : IAsyncDisposable
    {
        TContext Create();
    }
}