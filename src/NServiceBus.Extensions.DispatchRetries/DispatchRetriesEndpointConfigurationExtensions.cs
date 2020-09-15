namespace NServiceBus
{
    public static class DispatchRetriesEndpointConfigurationExtensions
    {
        public static DispatchRetriesConfiguration DispatchRetries(this EndpointConfiguration configuration)
        {
            return new DispatchRetriesConfiguration(configuration);
        }
    }
}