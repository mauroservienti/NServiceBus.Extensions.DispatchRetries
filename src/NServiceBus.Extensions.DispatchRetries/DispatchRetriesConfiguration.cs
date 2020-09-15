using System;
using NServiceBus.Extensions.DispatchRetries;
using Polly;

namespace NServiceBus
{
    public class DispatchRetriesConfiguration
    {
        private readonly EndpointConfiguration _configuration;

        internal DispatchRetriesConfiguration(EndpointConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void EnableImmediateDispatchRetries(AsyncPolicy immediateDispatchRetryPolicy)
        {
            if (immediateDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryPolicy));
            }

            _configuration.Pipeline.Register(new ImmediateDispatchRetriesBehavior(immediateDispatchRetryPolicy), "Immediate dispatch retries behavior based on Polly.");
        }

        public void EnableBatchDispatchRetries(AsyncPolicy batchDispatchRetryPolicy)
        {
            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            _configuration.Pipeline.Register(new BatchDispatchRetriesBehavior(batchDispatchRetryPolicy), "Batch dispatch retries behavior based on Polly.");
        }
    }
}