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
            Mock<ISaga<TestEvent, IDisposable>> saga,
            ILogger<Pipeline<TestEvent, IDisposable, TestResult>> logger,
            IDependencyProvider<IDisposable> dependencyProvider,
            Mock<IRetryScheme> retryScheme)
        {
            saga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IDisposable>()));

            retryScheme.Setup(x => x.Retry(It.IsAny<Func<Task>>()))
                .Callback<Func<Task>>(invocation =>
                {
                    invocation.Invoke().Wait();
                });

            var pipeline = new Pipeline<TestEvent, IDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IDisposable>>(new List<ISaga<TestEvent, IDisposable>>
                {
                    saga.Object
                }),
                context => new TestResult(),
                logger,
                dependencyProvider,
                new PipelineConfiguration(retryScheme.Object));

            await pipeline.Execute(new TestEvent());

            saga.Verify(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IDisposable>()));
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_RollbackCompletedSagas(
            Mock<ISaga<TestEvent, IDisposable>> saga,
            Mock<ISaga<TestEvent, IDisposable>> exceptionSaga,
            ILogger<Pipeline<TestEvent, IDisposable, TestResult>> logger,
            Mock<IDependencyProvider<IDisposable>> dependencyProvider,
            Mock<IRetryScheme> retryScheme)
        {
            saga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IDisposable>())).Returns(Task.CompletedTask);
            saga.Setup(x => x.Rollback(It.IsAny<TestEvent>(), It.IsAny<IDisposable>())).Returns(Task.CompletedTask);

            exceptionSaga.Setup(x => x.Rollback(It.IsAny<TestEvent>(), It.IsAny<IDisposable>()));
            exceptionSaga.Setup(x => x.Then(It.IsAny<TestEvent>(), It.IsAny<IDisposable>()))
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

            var pipeline = new Pipeline<TestEvent, IDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IDisposable>>(
                    new List<ISaga<TestEvent, IDisposable>>
                    {
                        saga.Object,
                        exceptionSaga.Object,
                    }),
                context => new TestResult(),
                logger,
                dependencyProvider.Object,
                new PipelineConfiguration(retryScheme.Object));

            await pipeline.Execute(new TestEvent());

            saga.Verify(x => x.Rollback(It.IsAny<TestEvent>(), It.IsAny<IDisposable>()));
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_MapStateToResult(
            IRetryScheme retryScheme,
            ILogger<Pipeline<TestEvent, IDisposable, TestResult>> logger,
            IDependencyProvider<IDisposable> dependencyProvider,
            TestResult result)
        {
            var pipeline = new Pipeline<TestEvent, IDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IDisposable>>(
                    new List<ISaga<TestEvent, IDisposable>>()),
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
            ILogger<Pipeline<TestEvent, IDisposable, TestResult>> logger,
            Mock<IDependencyProvider<IDisposable>> dependencyProvider,
            Mock<IDisposable> context)
        {
            context.Setup(x => x.Dispose());
            dependencyProvider.Setup(x => x.Create()).Returns(context.Object);

            var pipeline = new Pipeline<TestEvent, IDisposable, TestResult>(
                new ReadOnlyCollection<ISaga<TestEvent, IDisposable>>(new List<ISaga<TestEvent, IDisposable>>()),
                context => new TestResult(),
                logger,
                dependencyProvider.Object,
                new PipelineConfiguration(retryScheme));

            await pipeline.Execute(new TestEvent());

            context.Verify(x => x.Dispose());
        }

        public class TestResult { }

        public class TestContext : IDisposable
        {
            public void Dispose()
            {
                // do nothing
            }
        }

        public class TestEvent { }
    }
}