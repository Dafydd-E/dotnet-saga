using System;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Saga.RetrySchemes;
using Xunit;

namespace Saga.Tests.RetrySchemes
{
    public class ExponentialBackOffSchemeTests
    {
        [Theory]
        [AutoCustomData]
        public async Task Should_ApplyExponentialBackOffAlgorithm_WithGivenRetryCount(
            int retryCount,
            ILogger<ExponentialBackOffScheme> exponentialBackOffScheme,
            Mock<IScheduler> scheduler)
        {
            scheduler.Setup(x => x.Wait(It.IsAny<int>())).Returns(Task.CompletedTask);
            var retryScheme = new ExponentialBackOffScheme(retryCount, scheduler.Object, exponentialBackOffScheme);

            try
            {
                await retryScheme.Retry(() => throw new Exception());
            }
            catch
            {
                // do nothing
            }

            for (var i = 0; i < retryCount; i++)
            {
                scheduler.Verify(s => s.Wait(
                    It.IsInRange<int>(
                        2000 * (i + 1),
                        2000 * (i + 1) + 1000,
                        Moq.Range.Inclusive)));
            }
        }
    }
}