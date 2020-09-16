namespace NServiceBus
{
    public static class DispatchRetriesEndpointConfigurationExtensions
    {
        private static DispatchRetriesConfiguration _config;

        public static DispatchRetriesConfiguration DispatchRetries(this EndpointConfiguration configuration)
        {
            return _config ??= new DispatchRetriesConfiguration(configuration);
        }
    }
}