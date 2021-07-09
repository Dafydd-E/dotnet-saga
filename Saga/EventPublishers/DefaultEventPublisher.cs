using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Saga.Pipelines;

namespace Saga.EventPublishers
{
    public class DefaultEventPublisher
    {
        private static IServiceScopeFactory ServiceScopeFactory;

        public DefaultEventPublisher(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory == null)
            {
                ServiceScopeFactory = serviceScopeFactory;
            }
        }

        public static async Task<TResult> Publish<TEvent, TContext, TResult>(TEvent @event) where TContext : IAsyncDisposable
        {
            using var serviceScope = ServiceScopeFactory.CreateScope();

            var pipeline = serviceScope.ServiceProvider.GetRequiredService<Pipeline<TEvent, TContext, TResult>>();
            return await pipeline.Execute(@event);
        }
    }
}