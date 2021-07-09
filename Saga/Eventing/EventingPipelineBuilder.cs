using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Saga.RetrySchemes;

namespace Saga.Eventing
{
    public class EventingPipelineBuilder<TContext, TEvent, TResult> where TContext : IEventingSagaContext<TEvent>, IAsyncDisposable
    {
        private IList<IEventingSaga<TContext, TEvent>> sagas;
        private IDependencyProvider<TContext> dependencyProvider;
        private IRetryScheme retryScheme;
        private ILogger<EventingPipeline<TContext, TEvent, TResult>> logger;
        private Func<TContext, TResult> mapping;

        public EventingPipelineBuilder<TContext, TEvent, TResult> WithSagas(
            IEnumerable<IEventingSaga<TContext, TEvent>> sagas)
        {
            this.sagas = sagas.ToList();
            return this;
        }

        public EventingPipelineBuilder<TContext, TEvent, TResult> WithDependencyProvider(IDependencyProvider<TContext> dependencyProvider)
        {
            this.dependencyProvider = dependencyProvider;
            return this;
        }

        public EventingPipelineBuilder<TContext, TEvent, TResult> WithRetryScheme(IRetryScheme retryScheme)
        {
            this.retryScheme = retryScheme;
            return this;
        }

        public EventingPipelineBuilder<TContext, TEvent, TResult> WithLogger(ILogger<EventingPipeline<TContext, TEvent, TResult>> logger)
        {
            this.logger = logger;
            return this;
        }

        public EventingPipelineBuilder<TContext, TEvent, TResult> WithMapping(Func<TContext, TResult> mapping)
        {
            this.mapping = mapping;
            return this;
        }

        public EventingPipeline<TContext, TEvent, TResult> Build() => new EventingPipeline<TContext, TEvent, TResult>(
                new ReadOnlyCollection<IEventingSaga<TContext, TEvent>>(this.sagas),
                this.dependencyProvider,
                this.retryScheme,
                this.logger,
                this.mapping);
    }
}