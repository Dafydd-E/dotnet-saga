using Saga.RetrySchemes;

namespace Saga.Pipelines
{
    public class PipelineConfiguration
    {
        public IRetryScheme Scheme { get; }

        public PipelineConfiguration(IRetryScheme scheme)
        {
            this.Scheme = scheme;
        }
    }
}