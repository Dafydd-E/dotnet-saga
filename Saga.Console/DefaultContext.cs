using System;
using Microsoft.Extensions.DependencyInjection;

namespace Saga.Console
{
    public class DefaultContext : IDisposable
    {
        public IServiceScope ServiceScope { get; }

        public DefaultContext(IServiceScope serviceScope)
        {
            this.ServiceScope = serviceScope;
        }

        public void Dispose()
        {
            this.ServiceScope.Dispose();
        }
    }
}