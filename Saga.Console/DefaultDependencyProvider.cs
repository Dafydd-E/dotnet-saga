namespace Saga.Console
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class DefaultDependencyProvider : IDependencyProvider<DefaultContext>
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public DefaultDependencyProvider(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Can be used to initialize database context, start transactions etc, etc
        /// </summary>
        public DefaultContext Create()
        {
            return new DefaultContext(this.serviceScopeFactory.CreateScope());
        }
    }
}