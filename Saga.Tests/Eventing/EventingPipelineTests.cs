using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Saga.Eventing;
using Saga.Pipelines;
using Saga.RetrySchemes;
using Xunit;

namespace Saga.Tests.Eventing
{
    public class EventingPipelineTests
    {
        [Theory]
        [AutoCustomData]
        public async Task Should_InvokeSagaAction(
            ILogger<EventingPipeline<TestContext, TestEvent, TestResult>> logger,
            TestContextProvider dependencyProvider,
            Mock<IRetryScheme> retryScheme,
            Mock<EventHandler<TestContext>> run,
            Mock<EventHandler<TestContext>> rollback)
        {
            var saga = new Mock<TestSaga>(() => new TestSaga(run.Object, rollback.Object));
            retryScheme.Setup(x => x.Retry(It.IsAny<Func<Task>>()))
                .Callback<Func<Task>>(invocation =>
                {
                    invocation.Invoke().Wait();
                });

            var pipeline = new EventingPipeline<TestContext, TestEvent, TestResult>(
                new ReadOnlyCollection<IEventingSaga<TestContext, TestEvent>>(new List<IEventingSaga<TestContext, TestEvent>>
                {
                    saga.Object
                }),
                dependencyProvider,
                retryScheme.Object,
                logger,
                context => new TestResult());

            await pipeline.Execute(new TestEvent());

            run.Verify(x => x.Invoke(It.IsAny<object>(), It.IsAny<TestContext>()));
        }

        [Theory]
        [AutoCustomData]
        public async Task Should_RollbackCompletedSagas(
            ILogger<EventingPipeline<TestContext, TestEvent, TestResult>> logger,
            TestContextProvider dependencyProvider,
            Mock<IRetryScheme> retryScheme,
            Mock<EventHandler<TestContext>> run,
            Mock<EventHandler<TestContext>> rollback)
        {
            EventHandler<TestContext> exceptionRun = (object sender, TestContext context) => throw new Exception();
            var exceptionSaga = new TestSaga(
                exceptionRun,
                (object sender, TestContext context) => Console.WriteLine("rolling back"));

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

            var saga = new Mock<TestSaga>(() => new TestSaga(run.Object, rollback.Object));
            retryScheme.Setup(x => x.Retry(It.IsAny<Func<Task>>()))
                .Callback<Func<Task>>(invocation =>
                {
                    invocation.Invoke().Wait();
                });

            var pipeline = new EventingPipeline<TestContext, TestEvent, TestResult>(
                new ReadOnlyCollection<IEventingSaga<TestContext, TestEvent>>(new List<IEventingSaga<TestContext, TestEvent>>
                {
                    saga.Object,
                    exceptionSaga,
                }),
                dependencyProvider,
                retryScheme.Object,
                logger,
                context => new TestResult());

            await pipeline.Execute(new TestEvent());

            rollback.Verify(x => x.Invoke(It.IsAny<object>(), It.IsAny<TestContext>()));
        }

        public class TestResult { }

        public class TestContext : IAsyncDisposable, IEventingSagaContext<TestEvent>
        {
            public TestContext(TestEvent @event)
            {
                this.Event = @event;
            }

            public TestEvent Event { get; }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        public class TestEvent { }

        public class TestSaga : IEventingSaga<TestContext, TestEvent>
        {
            public TestSaga(EventHandler<TestContext> run, EventHandler<TestContext> rollback)
            {
                this.Run += run;
                this.Rollback += rollback;
            }

            public EventHandler<TestContext> Run { get; }

            public EventHandler<TestContext> Rollback { get; }
        }

        public class TestContextProvider : IDependencyProvider<TestContext>
        {
            public TestContext Create()
            {
                return new TestContext(new TestEvent());
            }
        }
    }
}