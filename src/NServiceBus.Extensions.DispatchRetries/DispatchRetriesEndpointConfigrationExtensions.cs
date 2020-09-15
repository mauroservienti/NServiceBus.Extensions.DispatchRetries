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
            EnableDispatchRetries(configuration, defaultRetryPolicy, defaultRetryPolicy);
        }

        public static void EnableDispatchRetries(this EndpointConfiguration configuration, AsyncPolicy batchDispatchRetryPolicy, AsyncPolicy immediateDispatchRetryPolicy)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            if (immediateDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryPolicy));
            }

            configuration.Pipeline.Register(new BatchDispatchRetriesBehavior(batchDispatchRetryPolicy), "Batch dispatch retries behavior based on Polly.");
            configuration.Pipeline.Register(new ImmediateDispatchRetriesBehavior(immediateDispatchRetryPolicy), "Immediate dispatch retries behavior based on Polly.");
        }
    }
}