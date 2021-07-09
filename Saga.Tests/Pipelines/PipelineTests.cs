using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Saga.Pipelines;
using Saga.RetrySchemes;
using Xunit;

namespace Saga.Tests.Pipelines
{
    public class PipelineTests
    {
        [Theory]
        [AutoCustomData]
        public async Task Should_InvokeSagaAction(
            Mock<ISaga<TestEvent, IAsyncDisposable>> saga,
            ILogger<Pipeline<TestEvent, IAsyncDisposable, TestResult>> logger,
            IDependencyProvider<IAsyncDisposable> dependencyProvider,
            Mock<IRetryScheme> retryScheme)
        {
            saga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>()));

            retryScheme.Setup(x => x.Retry(It.IsAny<Func<Task>>()))
                .Callback<Func<Task>>(invocation =>
                {
                    invocation.Invoke().Wait();
                });

            var pipeline = new Pipeline<TestEvent, IAsyncDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IAsyncDisposable>>(new List<ISaga<TestEvent, IAsyncDisposable>>
                {
                    saga.Object
                }),
                context => new TestResult(),
                logger,
                dependencyProvider,
                new PipelineConfiguration(retryScheme.Object));

            await pipeline.Execute(new TestEvent());

            saga.Verify(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>()));
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_RollbackCompletedSagas(
            Mock<ISagaWithRollback<TestEvent, IAsyncDisposable>> saga,
            Mock<ISaga<TestEvent, IAsyncDisposable>> exceptionSaga,
            ILogger<Pipeline<TestEvent, IAsyncDisposable, TestResult>> logger,
            Mock<IDependencyProvider<IAsyncDisposable>> dependencyProvider,
            Mock<IRetryScheme> retryScheme)
        {
            saga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>())).Returns(Task.CompletedTask);
            saga.Setup(x => x.Rollback(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>())).Returns(Task.CompletedTask);

            exceptionSaga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>()))
                .Throws<Exception>();

            retryScheme
                .Setup(x => x.Retry(It.IsAny<Func<Task>>()))
                .Callback(new InvocationAction((invocation) =>
                {
                    var function = invocation.Arguments[0] as Func<Task>;
                    if (function == null)
                    {
                        throw new InvalidOperationException();
                    }

                    function.Invoke();
                }));

            var pipeline = new Pipeline<TestEvent, IAsyncDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IAsyncDisposable>>(
                    new List<ISaga<TestEvent, IAsyncDisposable>>
                    {
                        saga.Object,
                        exceptionSaga.Object,
                    }),
                context => new TestResult(),
                logger,
                dependencyProvider.Object,
                new PipelineConfiguration(retryScheme.Object));

            await pipeline.Execute(new TestEvent());

            saga.Verify(x => x.Rollback(It.IsAny<TestEvent>(), It.IsAny<IAsyncDisposable>()));
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_MapStateToResult(
            IRetryScheme retryScheme,
            ILogger<Pipeline<TestEvent, IAsyncDisposable, TestResult>> logger,
            IDependencyProvider<IAsyncDisposable> dependencyProvider,
            TestResult result)
        {
            var pipeline = new Pipeline<TestEvent, IAsyncDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IAsyncDisposable>>(
                    new List<ISaga<TestEvent, IAsyncDisposable>>()),
                context => result,
                logger,
                dependencyProvider,
                new PipelineConfiguration(retryScheme));

            var executionResult = await pipeline.Execute(new TestEvent());
            executionResult.Should().Be(result);
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_DisposeOfContext(
            IRetryScheme retryScheme,
            ILogger<Pipeline<TestEvent, IAsyncDisposable, TestResult>> logger,
            Mock<IDependencyProvider<IAsyncDisposable>> dependencyProvider,
            Mock<IAsyncDisposable> context)
        {
            context.Setup(x => x.DisposeAsync());
            dependencyProvider.Setup(x => x.Create()).Returns(context.Object);

            var pipeline = new Pipeline<TestEvent, IAsyncDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IAsyncDisposable>>(new List<ISaga<TestEvent, IAsyncDisposable>>()),
                context => new TestResult(),
                logger,
                dependencyProvider.Object,
                new PipelineConfiguration(retryScheme));

            await pipeline.Execute(new TestEvent());

            context.Verify(x => x.DisposeAsync());
        }

        public class TestResult { }

        public class TestContext : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        public class TestEvent { }
    }
}