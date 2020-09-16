using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.DispatchRetries;
using NServiceBus.Extensions.DispatchRetries.Features;
using Polly;

namespace NServiceBus
{
    public class DispatchRetriesConfiguration
    {
        private readonly EndpointConfiguration _configuration;

        internal DispatchRetriesConfiguration(EndpointConfiguration configuration)
        {
            _configuration = configuration;

            configuration.EnableFeature<DispatchRetriesFeature>();
        }

        public void DefaultImmediateDispatchRetriesPolicy(AsyncPolicy immediateDispatchRetryPolicy)
        {
            if (immediateDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryPolicy));
            }

            _configuration.GetSettings().Set(Constants.DefaultImmediateDispatchRetryPolicy, immediateDispatchRetryPolicy);
        }

        public void DefaultBatchDispatchRetriesPolicy(AsyncPolicy batchDispatchRetryPolicy)
        {
            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            _configuration.GetSettings().Set(Constants.DefaultBatchDispatchRetryPolicy, batchDispatchRetryPolicy);
        }
    }
}