using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Saga.Pipelines
{
    public class Pipeline<TEvent, TContext, TResult> where TContext : IAsyncDisposable
    {
        private readonly IReadOnlyList<ISaga<TEvent, TContext>> sagas;
        private readonly Func<TContext, TResult> mapFunction;
        private readonly ILogger<Pipeline<TEvent, TContext, TResult>> logger;
        private readonly IDependencyProvider<TContext> dependencyProvider;
        private readonly PipelineConfiguration configuration;

        public Pipeline(
            IReadOnlyList<ISaga<TEvent, TContext>> sagas,
            Func<TContext, TResult> mapFunction,
            ILogger<Pipeline<TEvent, TContext, TResult>> logger,
            IDependencyProvider<TContext> dependencyProvider,
            PipelineConfiguration configuration)
        {
            this.sagas = sagas;
            this.mapFunction = mapFunction;
            this.logger = logger;
            this.dependencyProvider = dependencyProvider;
            this.configuration = configuration;
        }

        public async Task<TResult> Execute(TEvent @event)
        {
            await using var context = this.dependencyProvider.Create();
            var completedSagas = new List<ISaga<TEvent, TContext>>();
            foreach (var saga in this.sagas)
            {
                try
                {
                    await this.configuration.Scheme.Retry(() => saga.Then(@event, context));
                    completedSagas.Add(saga);
                }
                catch (Exception)
                {
                    foreach (var completedSaga in completedSagas)
                    {
                        if (completedSaga is ISagaWithRollback<TEvent, TContext> sagaWithRollback)
                        {
                            this.logger.LogDebug("Rolling back transaction");
                            await sagaWithRollback.Rollback(@event, context);
                        }
                    }
                }
            }

            return this.mapFunction(context);
        }
    }
}