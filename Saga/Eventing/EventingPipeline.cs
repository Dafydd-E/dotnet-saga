using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saga.RetrySchemes;

namespace Saga.Eventing
{
    public class EventingPipeline<TContext, TEvent, TResult> where TContext : IEventingSagaContext<TEvent>, IAsyncDisposable
    {
        private readonly IReadOnlyList<IEventingSaga<TContext, TEvent>> sagas;
        private readonly IDependencyProvider<TContext> dependencyProvider;
        private readonly IRetryScheme retryScheme;
        private readonly ILogger<EventingPipeline<TContext, TEvent, TResult>> logger;
        private readonly Func<TContext, TResult> mappingFunction;

        public EventingPipeline(
            IReadOnlyList<IEventingSaga<TContext, TEvent>> sagas,
            IDependencyProvider<TContext> dependencyProvider,
            IRetryScheme retryScheme,
            ILogger<EventingPipeline<TContext, TEvent, TResult>> logger,
            Func<TContext, TResult> mapping)
        {
            this.sagas = sagas;
            this.dependencyProvider = dependencyProvider;
            this.retryScheme = retryScheme;
            this.logger = logger;
            this.mappingFunction = mapping;
        }

        public async Task<TResult> Execute(TEvent @event)
        {
            await using var context = this.dependencyProvider.Create();

            var completedSagas = new List<IEventingSaga<TContext, TEvent>>();
            foreach (var saga in sagas)
            {
                try
                {
                    await this.retryScheme.Retry(() => Task.Run(() => saga.Run.Invoke(this, context)));
                    completedSagas.Add(saga);
                }
                catch (Exception)
                {
                    completedSagas.Reverse();
                    foreach (var completedSaga in completedSagas)
                    {
                        completedSaga.Rollback?.Invoke(this, context);
                    }
                }
            }

            return this.mappingFunction(context);
        }
    }
}