using System;

namespace Saga
{
    public interface IDependencyProvider<TContext> where TContext : IDisposable
    {
        TContext Create();
    }
}