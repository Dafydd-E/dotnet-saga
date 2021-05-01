using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Saga.RetrySchemes
{
    public class ExponentialBackOffScheme : IRetryScheme
    {
        private readonly int maximumRetries;
        private readonly ILogger<ExponentialBackOffScheme> logger;
        private readonly Random random = new();
        private readonly IScheduler scheduler;

        public ExponentialBackOffScheme(
            int maximumRetries,
            IScheduler scheduler,
            ILogger<ExponentialBackOffScheme> logger)
        {
            this.maximumRetries = maximumRetries;
            this.logger = logger;
            this.scheduler = scheduler;
        }

        public async Task Retry(Func<Task> action)
        {
            var correlationId = Guid.NewGuid();
            for (var i = 0; i < maximumRetries; i++)
            {
                try
                {
                    await action.Invoke();
                    return;
                }
                catch (Exception e)
                {
                    this.logger.LogWarning(e, "Retry failed on try {}. Correlation Id {}", i, correlationId);
                    var waitFor = (2000 * (i + 1)) + this.random.Next(1000);

                    this.logger.LogDebug("Waiting for {}", waitFor);
                    await this.scheduler.Wait(waitFor);
                }
            }

            this.logger.LogError("All retries failed for action. Correlation Id {}", correlationId);
            throw new Exception("All retries failed");
        }
    }
}