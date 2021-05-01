using System;
using System.Threading.Tasks;

namespace Saga.RetrySchemes
{
    public interface IRetryScheme
    {
        Task Retry(Func<Task> action);
    }
}