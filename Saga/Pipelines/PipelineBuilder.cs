using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Saga.Pipelines
{
    public class PipelineBuilder<TEvent, TContext, TResult> where TContext : IDisposable
    {
        private readonly List<ISaga<TEvent, TContext>> sagas = new();
        private Func<TContext, TResult> mapFunction;
        private PipelineConfiguration configuration;
        private IDependencyProvider<TContext> dependencyProvider;
        private ILogger<Pipeline<TEvent, TContext, TResult>> logger;

        public PipelineBuilder<TEvent, TContext, TResult> AddSaga(ISaga<TEvent, TContext> saga)
        {
            this.sagas.Add(saga);
            return this;
        }

        public PipelineBuilder<TEvent, TContext, TResult> WithMapFunction(Func<TContext, TResult> mapFunction)
        {
            this.mapFunction = mapFunction;
            return this;
        }

        public PipelineBuilder<TEvent, TContext, TResult> WithConfiguration(PipelineConfiguration configuration)
        {
            this.configuration = configuration;
            return this;
        }

        public PipelineBuilder<TEvent, TContext, TResult> WithLogger(ILogger<Pipeline<TEvent, TContext, TResult>> logger)
        {
            this.logger = logger;
            return this;
        }

        public PipelineBuilder<TEvent, TContext, TResult> WithDependencyProvider(IDependencyProvider<TContext> dependencyProvider)
        {
            this.dependencyProvider = dependencyProvider;
            return this;
        }

        public PipelineBuilder<TEvent, TContext, TResult> AddSagas(IEnumerable<ISaga<TEvent, TContext>> sagas)
        {
            this.sagas.AddRange(sagas);
            return this;
        }

        public Pipeline<TEvent, TContext, TResult> Build()
        {
            return new Pipeline<TEvent, TContext, TResult>(
                new ReadOnlyCollection<ISaga<TEvent, TContext>>(this.sagas),
                this.mapFunction,
                this.logger,
                this.dependencyProvider,
                this.configuration);
        }
    }
}