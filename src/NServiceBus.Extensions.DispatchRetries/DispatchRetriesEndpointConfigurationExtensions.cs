using Polly;

namespace NServiceBus
{
    public static class DispatchRetriesEndpointConfigurationExtensions
    {
        public static DispatchRetriesConfiguration DispatchRetries(this EndpointConfiguration configuration)
        {
            return new DispatchRetriesConfiguration(configuration);
        }

        public static DispatchRetriesConfiguration DispatchRetries(this EndpointConfiguration configuration, AsyncPolicy defaultBatchAndImmediateDispatchRetriesPolicy)
        {
            return new DispatchRetriesConfiguration(configuration, defaultBatchAndImmediateDispatchRetriesPolicy);
        }
    }
}