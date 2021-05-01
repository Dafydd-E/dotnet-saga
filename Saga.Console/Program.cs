namespace Saga.Console
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Saga.EventPublishers;
    using Saga.Pipelines;
    using Saga.RetrySchemes;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            IHost host = CreateHostBuilder(args).Build();

            var pipeline = host.Services.GetRequiredService<Pipeline<Event, DefaultContext, Result>>();
            var result = await pipeline.Execute(new Event());

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Pipeline finished with result {@Result}", result);
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging(loggingBuilder => loggingBuilder
                        .AddConsole()
                        .AddDebug());

                    services.AddTransient<IDependencyProvider<DefaultContext>, DefaultDependencyProvider>()
                        .AddTransient<ISaga<Event, DefaultContext>, ConsoleSaga>()
                        .AddTransient<ISaga<Event, DefaultContext>, ExceptionSaga>()
                        .AddTransient<IScheduler, DefaultScheduler>();

                    services.AddTransient<Pipeline<Event, DefaultContext, Result>>(
                        (serviceProvider) => new PipelineBuilder<Event, DefaultContext, Result>()
                            .WithLogger(serviceProvider.GetRequiredService<ILogger<Pipeline<Event, DefaultContext, Result>>>())
                            .WithDependencyProvider(serviceProvider.GetRequiredService<IDependencyProvider<DefaultContext>>())
                            .WithMapFunction((context) => new Result())
                            .WithConfiguration(new PipelineConfiguration(
                                new ExponentialBackOffScheme(
                                    5,
                                    serviceProvider.GetRequiredService<IScheduler>(),
                                    serviceProvider.GetRequiredService<ILogger<ExponentialBackOffScheme>>())))
                            .AddSagas(serviceProvider.GetServices<ISaga<Event, DefaultContext>>())
                            .Build());

                    var provider = services.BuildServiceProvider();
                    services.AddSingleton<DefaultEventPublisher>(new DefaultEventPublisher(provider.GetRequiredService<IServiceScopeFactory>()));
                });
        }
    }

    public class ConsoleSaga : ISaga<Event, DefaultContext>
    {
        public bool Retry => false;

        private readonly ILogger<ConsoleSaga> logger;

        public ConsoleSaga(ILogger<ConsoleSaga> logger)
        {
            this.logger = logger;
        }

        public Task Rollback(Event @event, DefaultContext state)
        {
            this.logger.LogInformation("Rolling back {}", nameof(ConsoleSaga));
            return Task.CompletedTask;
        }

        public Task Then(Event @event, DefaultContext state)
        {
            this.logger.LogInformation("Executing Saga for event");
            return Task.CompletedTask;
        }
    }

    public class ExceptionSaga : ISaga<Event, DefaultContext>
    {
        public bool Retry => true;

        public Task Rollback(Event @event, DefaultContext state)
        {
            return Task.CompletedTask;
        }

        public Task Then(Event @event, DefaultContext state)
        {
            throw new NotImplementedException();
        }
    }

    public class Event { }

    public class Result { }
}
