using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.DispatchRetries;
using Polly;

namespace NServiceBus
{
    public static class DispatchRetriesEndpointConfigrationExtensions
    {
        public static void EnableDispatchRetries(this EndpointConfiguration configuration)
        {
            var defaultPolicy = Policy.NoOpAsync();
            EnableDispatchRetries(configuration, defaultPolicy);
        }

        public static void EnableDispatchRetries(this EndpointConfiguration configuration, AsyncPolicy defaultRetryPolicy)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            if (defaultRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(defaultRetryPolicy));
            }
            
            configuration.Pipeline.Register(new DispatchRetriesBehavior(defaultRetryPolicy), "Dispatch retries behavior based on Polly.");
        }
    }
}